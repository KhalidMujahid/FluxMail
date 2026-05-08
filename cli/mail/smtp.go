package mail

import (
	"context"
	"fmt"

	"gopkg.in/gomail.v2"

	"github.com/fluxmail/cli/models"
)

type SMTPSender struct{}

func (s *SMTPSender) Send(_ context.Context, p *models.Provider, msg *models.Message) models.SendResult {
	m := gomail.NewMessage()

	fromName := msg.FromNameOverride
	if fromName == "" {
		fromName = p.SenderName
	}
	m.SetAddressHeader("From", p.SenderEmail, fromName)
	m.SetAddressHeader("To", msg.ToEmail, msg.ToName)
	m.SetHeader("Subject", msg.Subject)
	if msg.ReplyTo != "" {
		m.SetHeader("Reply-To", msg.ReplyTo)
	}
	m.SetBody("text/html", msg.HtmlBody)
	if msg.PlainTextBody != "" {
		m.AddAlternative("text/plain", msg.PlainTextBody)
	}

	d := gomail.NewDialer(p.SmtpHost, p.SmtpPort, p.SmtpUsername, p.SmtpPassword)
	d.SSL = p.SmtpUseSsl

	if err := d.DialAndSend(m); err != nil {
		return models.SendResult{Err: fmt.Errorf("smtp: %w", err)}
	}
	return models.SendResult{}
}
