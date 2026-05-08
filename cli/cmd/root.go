package cmd

import (
	"database/sql"
	"fmt"
	"os"
	"path/filepath"

	"github.com/spf13/cobra"

	dbpkg "github.com/fluxmail/cli/db"
)

var (
	dbPath string
	DB     *sql.DB
)

var rootCmd = &cobra.Command{
	Use:   "fluxmail",
	Short: "FluxMail CLI — send emails from your terminal",
	Long: `Use 'fluxmail --help' to see available commands.`,
	PersistentPreRunE: func(cmd *cobra.Command, args []string) error {
		if dbPath == "" {
			cfgDir, err := os.UserConfigDir()
			if err != nil {
				return fmt.Errorf("cannot locate config dir: %w", err)
			}
			dbPath = filepath.Join(cfgDir, "FluxMail", "fluxmail.db")
		}
		if _, err := os.Stat(dbPath); os.IsNotExist(err) {
			return fmt.Errorf("database not found at %s\nLaunch the FluxMail desktop app first to initialise it", dbPath)
		}
		var err error
		DB, err = dbpkg.Open(dbPath)
		if err != nil {
			return fmt.Errorf("cannot open database: %w", err)
		}
		return nil
	},
}

var configCmd = &cobra.Command{
	Use:   "config",
	Short: "Show current configuration",
	RunE: func(cmd *cobra.Command, args []string) error {
		if dbPath == "" {
			cfgDir, _ := os.UserConfigDir()
			dbPath = filepath.Join(cfgDir, "FluxMail", "fluxmail.db")
		}
		fmt.Printf("Database : %s\n", dbPath)
		if _, err := os.Stat(dbPath); os.IsNotExist(err) {
			fmt.Println("Status   : not found — launch the desktop app to create it")
		} else {
			fmt.Println("Status   : found")
		}
		return nil
	},
}

func Execute() {
	if err := rootCmd.Execute(); err != nil {
		os.Exit(1)
	}
}

func init() {
	rootCmd.PersistentFlags().StringVar(&dbPath, "db", "",
		"path to fluxmail.db (default: %AppData%/FluxMail/fluxmail.db)")

	rootCmd.AddCommand(configCmd)
	rootCmd.AddCommand(sendCmd)
	rootCmd.AddCommand(bulkCmd)
	rootCmd.AddCommand(providersCmd)
	rootCmd.AddCommand(templatesCmd)
	rootCmd.AddCommand(contactsCmd)
}

func coalesce(a, b string) string {
	if a != "" {
		return a
	}
	return b
}
