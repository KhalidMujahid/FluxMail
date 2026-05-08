package cmd

import (
	"bufio"
	"context"
	"fmt"
	"os"
	"strings"
	"time"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
	"github.com/fluxmail/cli/mail"
	"github.com/fluxmail/cli/models"
)

var (
	bulkFile        string
	bulkSubject     string
	bulkHtml        string
	bulkHtmlFile    string
	bulkProvider    string
	bulkFromName    string
	bulkTemplate    int
	bulkDelay       float64
	bulkDryRun      bool
	bulkContactList int
)

var bulkCmd = &cobra.Command{
	Use:   "bulk",
	Short: "Send an email to many recipients",
	Example: `  # From a plain-text file (one email per line)
  fluxmail bulk --file recipients.txt --subject "News" --html "<p>Hello!</p>"

  # From a CSV file + saved template
  fluxmail bulk --file list.csv --template 1

  # From a contact list stored in the app
  fluxmail bulk --list 2 --subject "Update" --html-file body.html --delay 2

  # Dry run — print recipients without sending
  fluxmail bulk --file recipients.txt --subject "Test" --html "<p>hi</p>" --dry-run`,
	RunE: runBulk,
}

func init() {
	bulkCmd.Flags().StringVarP(&bulkFile, "file", "f", "", "Recipients file (.txt one per line, or .csv)")
	bulkCmd.Flags().IntVar(&bulkContactList, "list", 0, "Send to a contact list by ID")
	bulkCmd.Flags().StringVarP(&bulkSubject, "subject", "s", "", "Email subject")
	bulkCmd.Flags().StringVar(&bulkHtml, "html", "", "HTML body (inline)")
	bulkCmd.Flags().StringVar(&bulkHtmlFile, "html-file", "", "Path to an HTML file to use as body")
	bulkCmd.Flags().StringVarP(&bulkProvider, "provider", "p", "", "Provider ID or name")
	bulkCmd.Flags().StringVar(&bulkFromName, "from-name", "", "Override sender display name")
	bulkCmd.Flags().IntVar(&bulkTemplate, "template", 0, "Template ID")
	bulkCmd.Flags().Float64VarP(&bulkDelay, "delay", "d", 1.0, "Seconds to wait between emails (0 = no delay)")
	bulkCmd.Flags().BoolVar(&bulkDryRun, "dry-run", false, "List recipients without sending")
}

type recipient struct {
	email string
	name  string
}

func runBulk(_ *cobra.Command, _ []string) error {
	if bulkFile == "" && bulkContactList == 0 {
		return fmt.Errorf("provide --file <path> or --list <id>")
	}

	var recs []recipient

	if bulkContactList > 0 {
		contacts, err := dbpkg.GetContacts(DB, bulkContactList)
		if err != nil {
			return fmt.Errorf("get contact list: %w", err)
		}
		for _, c := range contacts {
			recs = append(recs, recipient{email: c.Email, name: c.Name})
		}
	}

	if bulkFile != "" {
		fromFile, err := parseRecipientsFile(bulkFile)
		if err != nil {
			return err
		}
		recs = append(recs, fromFile...)
	}

	seen := map[string]bool{}
	var unique []recipient
	for _, r := range recs {
		key := strings.ToLower(r.email)
		if !seen[key] {
			seen[key] = true
			unique = append(unique, r)
		}
	}
	recs = unique

	if len(recs) == 0 {
		return fmt.Errorf("no valid recipients found")
	}

	var subject, htmlBody, plainBody string

	if bulkTemplate > 0 {
		tmpl, err := dbpkg.GetTemplateByID(DB, bulkTemplate)
		if err != nil {
			return err
		}
		subject, htmlBody, plainBody = tmpl.Subject, tmpl.HtmlBody, tmpl.PlainTextBody
	}
	if bulkSubject != "" {
		subject = bulkSubject
	}
	if bulkHtmlFile != "" {
		b, err := os.ReadFile(bulkHtmlFile)
		if err != nil {
			return fmt.Errorf("read --html-file: %w", err)
		}
		htmlBody = string(b)
	} else if bulkHtml != "" {
		htmlBody = bulkHtml
	}

	if subject == "" {
		return fmt.Errorf("--subject is required (or use --template)")
	}
	if htmlBody == "" {
		return fmt.Errorf("--html or --html-file is required (or use --template)")
	}

	prov, err := resolveProvider(bulkProvider)
	if err != nil {
		return err
	}

	fmt.Printf("Provider   : %s (%s)\n", prov.Name, prov.Type)
	fmt.Printf("From       : %s\n", formatFrom(&models.Message{FromNameOverride: bulkFromName}, prov))
	fmt.Printf("Subject    : %s\n", subject)
	fmt.Printf("Recipients : %d\n", len(recs))
	fmt.Printf("Delay      : %.1fs between emails\n", bulkDelay)
	fmt.Println()

	if bulkDryRun {
		fmt.Println("[dry-run] Recipients (no email sent):")
		for i, r := range recs {
			if r.name != "" {
				fmt.Printf("  %d. %s <%s>\n", i+1, r.name, r.email)
			} else {
				fmt.Printf("  %d. %s\n", i+1, r.email)
			}
		}
		return nil
	}

	sender, err := mail.NewSender(prov.Type)
	if err != nil {
		return err
	}

	success, failed := 0, 0
	total := len(recs)

	for i, r := range recs {
		msg := &models.Message{
			ToEmail:         r.email,
			ToName:          r.name,
			Subject:         subject,
			HtmlBody:        htmlBody,
			PlainTextBody:   plainBody,
			FromNameOverride: bulkFromName,
		}

		result := sender.Send(context.Background(), prov, msg)
		if result.Err != nil {
			fmt.Printf("[%d/%d] ✗  %s — %v\n", i+1, total, r.email, result.Err)
			failed++
		} else {
			fmt.Printf("[%d/%d] ✓  %s\n", i+1, total, r.email)
			success++
		}

		if bulkDelay > 0 && i < total-1 {
			time.Sleep(time.Duration(bulkDelay * float64(time.Second)))
		}
	}

	fmt.Printf("\nDone: %d sent, %d failed of %d total.\n", success, failed, total)
	return nil
}

func parseRecipientsFile(path string) ([]recipient, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, fmt.Errorf("open file: %w", err)
	}
	defer f.Close()

	isCSV := strings.HasSuffix(strings.ToLower(path), ".csv")
	var recs []recipient

	if isCSV {
		recs, err = parseCSV(f)
	} else {
		recs, err = parseTXT(f)
	}
	return recs, err
}

func parseTXT(f *os.File) ([]recipient, error) {
	var recs []recipient
	sc := bufio.NewScanner(f)
	for sc.Scan() {
		line := strings.TrimSpace(sc.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}

		if i := strings.LastIndex(line, "<"); i >= 0 {
			if j := strings.LastIndex(line, ">"); j > i {
				email := strings.TrimSpace(line[i+1 : j])
				name := strings.TrimSpace(line[:i])
				if strings.Contains(email, "@") {
					recs = append(recs, recipient{email: email, name: name})
					continue
				}
			}
		}

		for _, part := range strings.FieldsFunc(line, func(c rune) bool { return c == ',' || c == ';' }) {
			email := strings.TrimSpace(part)
			if strings.Contains(email, "@") {
				recs = append(recs, recipient{email: email})
			}
		}
	}
	return recs, sc.Err()
}

func parseCSV(f *os.File) ([]recipient, error) {
	sc := bufio.NewScanner(f)
	emailCol, nameCol := -1, -1
	var recs []recipient
	firstLine := true

	for sc.Scan() {
		fields := splitCSVLine(sc.Text())
		if firstLine {
			firstLine = false
			for i, fld := range fields {
				lower := strings.ToLower(strings.Trim(fld, `"`))
				if strings.Contains(lower, "email") && emailCol < 0 {
					emailCol = i
				}
				if strings.Contains(lower, "name") && nameCol < 0 {
					nameCol = i
				}
			}
			if emailCol < 0 {
				emailCol = 0
			}
			continue
		}
		if emailCol < len(fields) {
			email := strings.Trim(fields[emailCol], `"`)
			if !strings.Contains(email, "@") {
				continue
			}
			name := ""
			if nameCol >= 0 && nameCol < len(fields) {
				name = strings.Trim(fields[nameCol], `"`)
			}
			recs = append(recs, recipient{email: email, name: name})
		}
	}
	return recs, sc.Err()
}

func splitCSVLine(line string) []string {
	var fields []string
	var cur strings.Builder
	inQ := false
	for _, c := range line {
		switch {
		case c == '"':
			inQ = !inQ
		case c == ',' && !inQ:
			fields = append(fields, cur.String())
			cur.Reset()
		default:
			cur.WriteRune(c)
		}
	}
	return append(fields, cur.String())
}
