package cmd

import (
	"fmt"
	"strings"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
)

var providersCmd = &cobra.Command{
	Use:   "providers",
	Short: "List and inspect email providers",
}

var providersListCmd = &cobra.Command{
	Use:   "list",
	Short: "List all configured providers",
	RunE: func(_ *cobra.Command, _ []string) error {
		ps, err := dbpkg.GetProviders(DB)
		if err != nil {
			return err
		}
		if len(ps) == 0 {
			fmt.Println("No providers found. Add one in the FluxMail desktop app.")
			return nil
		}

		fmt.Printf("%-4s  %-22s %-10s %-8s %-8s  Sender\n", "ID", "Name", "Type", "Default", "Enabled")
		fmt.Println(strings.Repeat("─", 74))
		for _, p := range ps {
			def := "   "
			if p.IsDefault {
				def = " ✓ "
			}
			ena := " ✓ "
			if !p.IsEnabled {
				ena = " ✗ "
			}
			fmt.Printf("%-4d  %-22s %-10s %-8s %-8s  %s <%s>\n",
				p.ID, p.Name, p.Type, def, ena, p.SenderName, p.SenderEmail)
		}
		return nil
	},
}

var providerTestID int

var providersTestCmd = &cobra.Command{
	Use:   "test",
	Short: "Validate a provider's configuration",
	RunE: func(_ *cobra.Command, _ []string) error {
		var err error
		var prov interface{ GetID() int }
		_ = prov

		ps, err := dbpkg.GetProviders(DB)
		if err != nil {
			return err
		}

		var target *struct {
			ID    int
			Name  string
			Type  string
			valid bool
			notes []string
		}

		for _, p := range ps {
			if providerTestID != 0 && p.ID != providerTestID {
				continue
			}
			if providerTestID == 0 && !p.IsDefault {
				continue
			}

			target = &struct {
				ID    int
				Name  string
				Type  string
				valid bool
				notes []string
			}{ID: p.ID, Name: p.Name, Type: p.Type, valid: true}

			switch p.Type {
			case "Smtp":
				if p.SmtpHost == "" {
					target.valid = false
					target.notes = append(target.notes, "SmtpHost is not set")
				} else {
					target.notes = append(target.notes, fmt.Sprintf("SMTP host : %s:%d", p.SmtpHost, p.SmtpPort))
					target.notes = append(target.notes, fmt.Sprintf("Username  : %s", p.SmtpUsername))
					target.notes = append(target.notes, fmt.Sprintf("SSL       : %v", p.SmtpUseSsl))
				}
			case "Resend":
				if p.ResendApiKey == "" {
					target.valid = false
					target.notes = append(target.notes, "ResendApiKey is not set")
				} else {
					target.notes = append(target.notes, fmt.Sprintf("API key   : %s...", p.ResendApiKey[:min(8, len(p.ResendApiKey))]))
				}
			case "SendGrid":
				if p.SendGridApiKey == "" {
					target.valid = false
					target.notes = append(target.notes, "SendGridApiKey is not set")
				} else {
					target.notes = append(target.notes, fmt.Sprintf("API key   : %s...", p.SendGridApiKey[:min(8, len(p.SendGridApiKey))]))
				}
			case "AwsSes":
				if p.AwsAccessKeyId == "" {
					target.valid = false
					target.notes = append(target.notes, "AwsAccessKeyId is not set")
				} else {
					target.notes = append(target.notes, fmt.Sprintf("Region    : %s", p.AwsRegion))
					target.notes = append(target.notes, fmt.Sprintf("Access key: %s...", p.AwsAccessKeyId[:min(8, len(p.AwsAccessKeyId))]))
				}
			}
			break
		}

		if target == nil {
			if providerTestID != 0 {
				return fmt.Errorf("provider ID %d not found", providerTestID)
			}
			return fmt.Errorf("no default provider set — use --id to specify one")
		}

		status := "✓ configuration looks good"
		if !target.valid {
			status = "✗ configuration incomplete"
		}
		fmt.Printf("Provider : %s (ID %d, %s)\n%s\n", target.Name, target.ID, target.Type, status)
		for _, n := range target.notes {
			fmt.Printf("  %s\n", n)
		}
		return nil
	},
}

func init() {
	providersTestCmd.Flags().IntVar(&providerTestID, "id", 0, "Provider ID to inspect (uses default if omitted)")
	providersCmd.AddCommand(providersListCmd)
	providersCmd.AddCommand(providersTestCmd)
}
