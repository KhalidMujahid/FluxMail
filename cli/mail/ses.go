package mail

import (
	"bytes"
	"context"
	"fmt"
	"io"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/credentials"
	"github.com/aws/aws-sdk-go-v2/service/ses"
	"github.com/aws/aws-sdk-go-v2/service/ses/types"
	"gopkg.in/gomail.v2"

	"github.com/fluxmail/cli/models"
)

type SESSender struct{}

func (s *SESSender) Send(ctx context.Context, p *models.Provider, msg *models.Message) models.SendResult {
	region := p.AwsRegion
	if region == "" {
		region = "us-east-1"
	}

	// Build MIME message via gomail so custom headers are supported
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
	m.SetBody("text/plain", effectivePlainText(msg))
	m.AddAlternative("text/html", msg.HtmlBody)

	// Serialise to raw bytes without opening an SMTP connection
	var raw rawCollector
	if err := gomail.Send(&raw, m); err != nil {
		return models.SendResult{Err: fmt.Errorf("ses: build mime: %w", err)}
	}

	cfg := aws.Config{
		Region:      region,
		Credentials: credentials.NewStaticCredentialsProvider(p.AwsAccessKeyId, p.AwsSecretAccessKey, ""),
	}
	client := ses.NewFromConfig(cfg)

	input := &ses.SendRawEmailInput{
		RawMessage: &types.RawMessage{Data: raw.Bytes()},
	}
	out, err := client.SendRawEmail(ctx, input)
	if err != nil {
		return models.SendResult{Err: fmt.Errorf("ses: %w", err)}
	}
	return models.SendResult{MessageID: aws.ToString(out.MessageId)}
}

// rawCollector implements gomail.Sender by writing the MIME message into a buffer.
type rawCollector struct{ bytes.Buffer }

func (r *rawCollector) Send(_ string, _ []string, msg io.WriterTo) error {
	_, err := msg.WriteTo(r)
	return err
}
