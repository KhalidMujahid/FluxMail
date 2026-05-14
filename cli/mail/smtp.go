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
	m.SetHeader("List-Unsubscribe", unsubscribeHeader(msg, p))
	if msg.UnsubscribeUrl != "" {
		m.SetHeader("List-Unsubscribe-Post", "List-Unsubscribe=One-Click")
	}

	// plain/text first, text/html second — correct multipart/alternative order
	m.SetBody("text/plain", effectivePlainText(msg))
	m.AddAlternative("text/html", msg.HtmlBody)

	d := gomail.NewDialer(p.SmtpHost, p.SmtpPort, p.SmtpUsername, p.SmtpPassword)
	d.SSL = p.SmtpUseSsl

	if err := d.DialAndSend(m); err != nil {
		return models.SendResult{Err: fmt.Errorf("smtp: %w", err)}
	}
	return models.SendResult{}
}
