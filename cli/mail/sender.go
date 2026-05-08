package mail

import (
	"context"
	"fmt"

	"github.com/fluxmail/cli/models"
)

type Sender interface {
	Send(ctx context.Context, provider *models.Provider, msg *models.Message) models.SendResult
}

func NewSender(providerType string) (Sender, error) {
	switch providerType {
	case "Smtp":
		return &SMTPSender{}, nil
	case "Resend":
		return &ResendSender{}, nil
	case "SendGrid":
		return &SendGridSender{}, nil
	case "AwsSes":
		return &SESSender{}, nil
	default:
		return nil, fmt.Errorf("unsupported provider type %q", providerType)
	}
}

func formatAddress(name, email string) string {
	if name == "" {
		return email
	}
	return fmt.Sprintf("%s <%s>", name, email)
}

func effectiveFrom(msg *models.Message, p *models.Provider) string {
	name := msg.FromNameOverride
	if name == "" {
		name = p.SenderName
	}
	return formatAddress(name, p.SenderEmail)
}
