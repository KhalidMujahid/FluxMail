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
	}
	if msg.PlainTextBody != "" {
		params.Text = msg.PlainTextBody
	}
	if msg.ReplyTo != "" {
		params.ReplyTo = msg.ReplyTo
	}

	sent, err := client.Emails.Send(params)
	if err != nil {
		return models.SendResult{Err: fmt.Errorf("resend: %w", err)}
	}
	return models.SendResult{MessageID: sent.Id}
}
