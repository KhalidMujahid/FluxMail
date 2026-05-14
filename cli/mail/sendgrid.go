package mail

import (
	"context"
	"fmt"

	sendgrid "github.com/sendgrid/sendgrid-go"
	"github.com/sendgrid/sendgrid-go/helpers/mail"

	"github.com/fluxmail/cli/models"
)

type SendGridSender struct{}

func (s *SendGridSender) Send(_ context.Context, p *models.Provider, msg *models.Message) models.SendResult {
	fromName := msg.FromNameOverride
	if fromName == "" {
		fromName = p.SenderName
	}

	from := mail.NewEmail(fromName, p.SenderEmail)
	to := mail.NewEmail(msg.ToName, msg.ToEmail)

	m := mail.NewSingleEmail(from, msg.Subject, to, effectivePlainText(msg), msg.HtmlBody)
	if msg.ReplyTo != "" {
		m.ReplyTo = &mail.Email{Address: msg.ReplyTo}
	}

	if m.Headers == nil {
		m.Headers = make(map[string]string)
	}
	m.Headers["List-Unsubscribe"] = unsubscribeHeader(msg, p)
	if msg.UnsubscribeUrl != "" {
		m.Headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
	}

	client := sendgrid.NewSendClient(p.SendGridApiKey)
	resp, err := client.Send(m)
	if err != nil {
		return models.SendResult{Err: fmt.Errorf("sendgrid: %w", err)}
	}
	if resp.StatusCode >= 300 {
		return models.SendResult{Err: fmt.Errorf("sendgrid HTTP %d: %s", resp.StatusCode, resp.Body)}
	}

	var msgID string
	if ids, ok := resp.Headers["X-Message-Id"]; ok && len(ids) > 0 {
		msgID = ids[0]
	}
	return models.SendResult{MessageID: msgID}
}
