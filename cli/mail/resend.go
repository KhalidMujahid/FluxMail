package mail

import (
	"context"
	"fmt"

	"github.com/resend/resend-go/v2"

	"github.com/fluxmail/cli/models"
)

type ResendSender struct{}

func (s *ResendSender) Send(_ context.Context, p *models.Provider, msg *models.Message) models.SendResult {
	client := resend.NewClient(p.ResendApiKey)

	params := &resend.SendEmailRequest{
		From:    effectiveFrom(msg, p),
		To:      []string{formatAddress(msg.ToName, msg.ToEmail)},
		Subject: msg.Subject,
		Html:    msg.HtmlBody,
		Text:    effectivePlainText(msg),
	}
	if msg.ReplyTo != "" {
		params.ReplyTo = msg.ReplyTo
	}

	params.Headers = map[string]string{
		"List-Unsubscribe": unsubscribeHeader(msg, p),
	}
	if msg.UnsubscribeUrl != "" {
		params.Headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
	}

	sent, err := client.Emails.Send(params)
	if err != nil {
		return models.SendResult{Err: fmt.Errorf("resend: %w", err)}
	}
	return models.SendResult{MessageID: sent.Id}
}
