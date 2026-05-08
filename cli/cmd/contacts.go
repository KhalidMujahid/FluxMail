package cmd

import (
	"fmt"
	"strings"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
)

var contactsCmd = &cobra.Command{
	Use:   "contacts",
	Short: "Browse contacts and contact lists",
}

var (
	contactListID   int
	contactListName string
)

var contactsListCmd = &cobra.Command{
	Use:   "list",
	Short: "List contacts (all or filtered by list)",
	Example: `  fluxmail contacts list
  fluxmail contacts list --list "Newsletter"
  fluxmail contacts list --list-id 2`,
	RunE: func(_ *cobra.Command, _ []string) error {
		listID := contactListID

		if contactListName != "" && listID == 0 {
			lists, err := dbpkg.GetContactLists(DB)
			if err != nil {
				return err
			}
			for _, l := range lists {
				if strings.EqualFold(l.Name, contactListName) {
					listID = l.ID
					break
				}
			}
			if listID == 0 {
				return fmt.Errorf("contact list %q not found — run 'fluxmail contacts lists' to see available lists", contactListName)
			}
		}

		contacts, err := dbpkg.GetContacts(DB, listID)
		if err != nil {
			return err
		}
		if len(contacts) == 0 {
			fmt.Println("No contacts found.")
			return nil
		}

		fmt.Printf("%-4s  %-32s  %-22s  %s\n", "ID", "Email", "Name", "Company")
		fmt.Println(strings.Repeat("─", 80))
		for _, c := range contacts {
			email := c.Email
			if len(email) > 32 {
				email = email[:29] + "..."
			}
			name := c.Name
			if len(name) > 22 {
				name = name[:19] + "..."
			}
			fmt.Printf("%-4d  %-32s  %-22s  %s\n", c.ID, email, name, c.Company)
		}
		fmt.Printf("\n%d contact(s)\n", len(contacts))
		return nil
	},
}

var contactsListsCmd = &cobra.Command{
	Use:   "lists",
	Short: "List all contact lists",
	RunE: func(_ *cobra.Command, _ []string) error {
		lists, err := dbpkg.GetContactLists(DB)
		if err != nil {
			return err
		}
		if len(lists) == 0 {
			fmt.Println("No contact lists found.")
			return nil
		}
		fmt.Printf("%-4s  %s\n", "ID", "Name")
		fmt.Println(strings.Repeat("─", 40))
		for _, l := range lists {
			fmt.Printf("%-4d  %s\n", l.ID, l.Name)
		}
		fmt.Printf("\n%d list(s)\n", len(lists))
		return nil
	},
}

func init() {
	contactsListCmd.Flags().IntVar(&contactListID, "list-id", 0, "Filter by contact list ID")
	contactsListCmd.Flags().StringVar(&contactListName, "list", "", "Filter by contact list name")
	contactsCmd.AddCommand(contactsListCmd)
	contactsCmd.AddCommand(contactsListsCmd)
}
