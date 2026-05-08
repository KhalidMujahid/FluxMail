package cmd

import (
	"context"
	"fmt"
	"os"
	"strconv"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
	"github.com/fluxmail/cli/mail"
	"github.com/fluxmail/cli/models"
)

var (
	sendTo       string
	sendToName   string
	sendSubject  string
	sendHtml     string
	sendHtmlFile string
	sendProvider string
	sendFromName string
	sendReplyTo  string
	sendTemplate int
	sendDryRun   bool
)

var sendCmd = &cobra.Command{
	Use:   "send",
	Short: "Send a single email",
	Example: `  # Quick send with inline HTML
  fluxmail send --to bob@example.com --subject "Hello" --html "<p>Hi Bob!</p>"

  # Use a saved template
  fluxmail send --to bob@example.com --template 2

  # From an HTML file, specific provider, override sender name
  fluxmail send --to bob@example.com --subject "Newsletter" \
    --html-file ./newsletter.html --provider 3 --from-name "Marketing Team"

  # Dry run — print what would be sent without actually sending
  fluxmail send --to bob@example.com --subject "Test" --html "<p>test</p>" --dry-run`,
	RunE: runSend,
}

func init() {
	sendCmd.Flags().StringVarP(&sendTo, "to", "t", "", "Recipient email address (required)")
	sendCmd.Flags().StringVar(&sendToName, "to-name", "", "Recipient display name")
	sendCmd.Flags().StringVarP(&sendSubject, "subject", "s", "", "Email subject")
	sendCmd.Flags().StringVar(&sendHtml, "html", "", "HTML body (inline)")
	sendCmd.Flags().StringVar(&sendHtmlFile, "html-file", "", "Path to an HTML file to use as body")
	sendCmd.Flags().StringVarP(&sendProvider, "provider", "p", "", "Provider ID or exact name (uses default if omitted)")
	sendCmd.Flags().StringVar(&sendFromName, "from-name", "", "Override the sender display name for this send")
	sendCmd.Flags().StringVar(&sendReplyTo, "reply-to", "", "Reply-To email address")
	sendCmd.Flags().IntVar(&sendTemplate, "template", 0, "Template ID — pre-fills subject and body")
	sendCmd.Flags().BoolVar(&sendDryRun, "dry-run", false, "Print what would be sent without sending")
	_ = sendCmd.MarkFlagRequired("to")
}

func runSend(_ *cobra.Command, _ []string) error {
	msg := &models.Message{
		ToEmail:         sendTo,
		ToName:          sendToName,
		ReplyTo:         sendReplyTo,
		FromNameOverride: sendFromName,
	}

	if sendTemplate > 0 {
		tmpl, err := dbpkg.GetTemplateByID(DB, sendTemplate)
		if err != nil {
			return err
		}
		msg.Subject = tmpl.Subject
		msg.HtmlBody = tmpl.HtmlBody
		msg.PlainTextBody = tmpl.PlainTextBody
	}

	if sendSubject != "" {
		msg.Subject = sendSubject
	}
	if sendHtmlFile != "" {
		b, err := os.ReadFile(sendHtmlFile)
		if err != nil {
			return fmt.Errorf("read --html-file: %w", err)
		}
		msg.HtmlBody = string(b)
	} else if sendHtml != "" {
		msg.HtmlBody = sendHtml
	}

	if msg.Subject == "" {
		return fmt.Errorf("--subject is required (or use --template to load one)")
	}
	if msg.HtmlBody == "" {
		return fmt.Errorf("--html or --html-file is required (or use --template)")
	}

	prov, err := resolveProvider(sendProvider)
	if err != nil {
		return err
	}

	printSendSummary(prov, msg)

	if sendDryRun {
		fmt.Println("\n[dry-run] Email not sent.")
		return nil
	}

	sender, err := mail.NewSender(prov.Type)
	if err != nil {
		return err
	}

	result := sender.Send(context.Background(), prov, msg)
	if result.Err != nil {
		return fmt.Errorf("send failed: %w", result.Err)
	}

	if result.MessageID != "" {
		fmt.Printf("\n✓ Sent — message ID: %s\n", result.MessageID)
	} else {
		fmt.Println("\n✓ Sent successfully.")
	}
	return nil
}

func resolveProvider(flag string) (*models.Provider, error) {
	if flag == "" {
		return dbpkg.GetDefaultProvider(DB)
	}
	if id, err := strconv.Atoi(flag); err == nil {
		return dbpkg.GetProviderByID(DB, id)
	}
	return dbpkg.GetProviderByName(DB, flag)
}

func printSendSummary(prov *models.Provider, msg *models.Message) {
	fmt.Printf("Provider : %s (%s)\n", prov.Name, prov.Type)
	fmt.Printf("From     : %s\n", formatFrom(msg, prov))
	if msg.ToName != "" {
		fmt.Printf("To       : %s <%s>\n", msg.ToName, msg.ToEmail)
	} else {
		fmt.Printf("To       : %s\n", msg.ToEmail)
	}
	fmt.Printf("Subject  : %s\n", msg.Subject)
	fmt.Printf("Body     : %d bytes of HTML\n", len(msg.HtmlBody))
}

func formatFrom(msg *models.Message, prov *models.Provider) string {
	name := coalesce(msg.FromNameOverride, prov.SenderName)
	if name == "" {
		return prov.SenderEmail
	}
	return fmt.Sprintf("%s <%s>", name, prov.SenderEmail)
}
