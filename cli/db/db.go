package db

import (
	"database/sql"
	"fmt"

	_ "modernc.org/sqlite"

	"github.com/fluxmail/cli/models"
)

func Open(path string) (*sql.DB, error) {
	db, err := sql.Open("sqlite", path)
	if err != nil {
		return nil, fmt.Errorf("open db: %w", err)
	}
	if err := db.Ping(); err != nil {
		return nil, fmt.Errorf("ping db: %w", err)
	}
	return db, nil
}

func GetProviders(db *sql.DB) ([]models.Provider, error) {
	rows, err := db.Query(`
		SELECT
			Id,
			Name,
			COALESCE(Type,'Smtp'),
			COALESCE(IsDefault,0),
			COALESCE(IsEnabled,1),
			COALESCE(SenderName,''),
			COALESCE(SenderEmail,''),
			COALESCE(SmtpHost,''),
			COALESCE(SmtpPort,587),
			COALESCE(SmtpUsername,''),
			COALESCE(SmtpPassword,''),
			COALESCE(SmtpUseSsl,1),
			COALESCE(ResendApiKey,''),
			COALESCE(AwsAccessKeyId,''),
			COALESCE(AwsSecretAccessKey,''),
			COALESCE(AwsRegion,'us-east-1'),
			COALESCE(SendGridApiKey,''),
			COALESCE(MailgunApiKey,''),
			COALESCE(MailgunDomain,'')
		FROM Providers
		ORDER BY Id`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var ps []models.Provider
	for rows.Next() {
		var p models.Provider
		var isDefault, isEnabled, smtpUseSsl int
		if err := rows.Scan(
			&p.ID, &p.Name, &p.Type, &isDefault, &isEnabled,
			&p.SenderName, &p.SenderEmail,
			&p.SmtpHost, &p.SmtpPort, &p.SmtpUsername, &p.SmtpPassword, &smtpUseSsl,
			&p.ResendApiKey,
			&p.AwsAccessKeyId, &p.AwsSecretAccessKey, &p.AwsRegion,
			&p.SendGridApiKey,
			&p.MailgunApiKey, &p.MailgunDomain,
		); err != nil {
			return nil, err
		}
		p.IsDefault = isDefault == 1
		p.IsEnabled = isEnabled == 1
		p.SmtpUseSsl = smtpUseSsl == 1
		ps = append(ps, p)
	}
	return ps, rows.Err()
}

func GetProviderByID(db *sql.DB, id int) (*models.Provider, error) {
	ps, err := GetProviders(db)
	if err != nil {
		return nil, err
	}
	for i := range ps {
		if ps[i].ID == id {
			return &ps[i], nil
		}
	}
	return nil, fmt.Errorf("provider with ID %d not found", id)
}

func GetProviderByName(db *sql.DB, name string) (*models.Provider, error) {
	ps, err := GetProviders(db)
	if err != nil {
		return nil, err
	}
	for i := range ps {
		if ps[i].Name == name {
			return &ps[i], nil
		}
	}
	return nil, fmt.Errorf("provider %q not found", name)
}

func GetDefaultProvider(db *sql.DB) (*models.Provider, error) {
	ps, err := GetProviders(db)
	if err != nil {
		return nil, err
	}
	for i := range ps {
		if ps[i].IsDefault && ps[i].IsEnabled {
			return &ps[i], nil
		}
	}
	for i := range ps {
		if ps[i].IsEnabled {
			return &ps[i], nil
		}
	}
	return nil, fmt.Errorf("no providers configured — add one in the FluxMail desktop app")
}

func GetTemplates(db *sql.DB) ([]models.Template, error) {
	rows, err := db.Query(`
		SELECT Id, Name, Subject, HtmlBody, COALESCE(PlainTextBody,'')
		FROM Templates
		ORDER BY Name`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var ts []models.Template
	for rows.Next() {
		var t models.Template
		if err := rows.Scan(&t.ID, &t.Name, &t.Subject, &t.HtmlBody, &t.PlainTextBody); err != nil {
			return nil, err
		}
		ts = append(ts, t)
	}
	return ts, rows.Err()
}

func GetTemplateByID(db *sql.DB, id int) (*models.Template, error) {
	ts, err := GetTemplates(db)
	if err != nil {
		return nil, err
	}
	for i := range ts {
		if ts[i].ID == id {
			return &ts[i], nil
		}
	}
	return nil, fmt.Errorf("template with ID %d not found", id)
}

func GetContacts(db *sql.DB, listID int) ([]models.Contact, error) {
	var (
		rows *sql.Rows
		err  error
	)
	if listID > 0 {
		rows, err = db.Query(`
			SELECT c.Id, c.Email, COALESCE(c.Name,''), COALESCE(c.Company,'')
			FROM Contacts c
			JOIN ContactListMemberships m ON m.ContactId = c.Id
			WHERE m.ContactListId = ?
			ORDER BY c.Email`, listID)
	} else {
		rows, err = db.Query(`
			SELECT Id, Email, COALESCE(Name,''), COALESCE(Company,'')
			FROM Contacts
			ORDER BY Email`)
	}
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var cs []models.Contact
	for rows.Next() {
		var c models.Contact
		if err := rows.Scan(&c.ID, &c.Email, &c.Name, &c.Company); err != nil {
			return nil, err
		}
		cs = append(cs, c)
	}
	return cs, rows.Err()
}

func GetContactLists(db *sql.DB) ([]models.ContactList, error) {
	rows, err := db.Query(`SELECT Id, Name FROM ContactLists ORDER BY Name`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var ls []models.ContactList
	for rows.Next() {
		var l models.ContactList
		if err := rows.Scan(&l.ID, &l.Name); err != nil {
			return nil, err
		}
		ls = append(ls, l)
	}
	return ls, rows.Err()
}
