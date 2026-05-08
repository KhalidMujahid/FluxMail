package cmd

import (
	"fmt"
	"strings"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
)

var templatesCmd = &cobra.Command{
	Use:   "templates",
	Short: "Browse saved email templates",
}

var templatesListCmd = &cobra.Command{
	Use:   "list",
	Short: "List all templates",
	RunE: func(_ *cobra.Command, _ []string) error {
		ts, err := dbpkg.GetTemplates(DB)
		if err != nil {
			return err
		}
		if len(ts) == 0 {
			fmt.Println("No templates found. Create one in the FluxMail desktop app.")
			return nil
		}
		fmt.Printf("%-4s  %-28s  %s\n", "ID", "Name", "Subject")
		fmt.Println(strings.Repeat("─", 72))
		for _, t := range ts {
			subj := t.Subject
			if len(subj) > 38 {
				subj = subj[:35] + "..."
			}
			name := t.Name
			if len(name) > 28 {
				name = name[:25] + "..."
			}
			fmt.Printf("%-4d  %-28s  %s\n", t.ID, name, subj)
		}
		fmt.Printf("\n%d template(s)\n", len(ts))
		return nil
	},
}

var templateShowID int

var templateShowCmd = &cobra.Command{
	Use:   "show",
	Short: "Show template subject and body",
	Example: `  fluxmail templates show --id 3`,
	RunE: func(_ *cobra.Command, _ []string) error {
		if templateShowID == 0 {
			return fmt.Errorf("--id is required")
		}
		t, err := dbpkg.GetTemplateByID(DB, templateShowID)
		if err != nil {
			return err
		}
		fmt.Printf("ID      : %d\n", t.ID)
		fmt.Printf("Name    : %s\n", t.Name)
		fmt.Printf("Subject : %s\n", t.Subject)
		fmt.Printf("HTML    : %d bytes\n", len(t.HtmlBody))
		if t.PlainTextBody != "" {
			fmt.Printf("Plain   : %d bytes\n", len(t.PlainTextBody))
		}
		fmt.Println()
		fmt.Println(strings.Repeat("─", 60))
		fmt.Println(t.HtmlBody)
		return nil
	},
}

func init() {
	templateShowCmd.Flags().IntVar(&templateShowID, "id", 0, "Template ID to display")
	templatesCmd.AddCommand(templatesListCmd)
	templatesCmd.AddCommand(templateShowCmd)
}
