package models

type Provider struct {
	ID               int
	Name             string
	Type             string
	IsDefault        bool
	IsEnabled        bool
	SenderName       string
	SenderEmail      string
	SmtpHost     string
	SmtpPort     int
	SmtpUsername string
	SmtpPassword string
	SmtpUseSsl   bool
	ResendApiKey string
	AwsAccessKeyId     string
	AwsSecretAccessKey string
	AwsRegion          string
	SendGridApiKey string
	MailgunApiKey string
	MailgunDomain string
}

type Template struct {
	ID            int
	Name          string
	Subject       string
	HtmlBody      string
	PlainTextBody string
}

type Contact struct {
	ID      int
	Email   string
	Name    string
	Company string
}

type ContactList struct {
	ID   int
	Name string
}

type Message struct {
	ToEmail         string
	ToName          string
	Subject         string
	HtmlBody        string
	PlainTextBody   string
	ReplyTo         string
	FromNameOverride string
}

type SendResult struct {
	MessageID string
	Err       error
}

func (r SendResult) Success() bool { return r.Err == nil }
