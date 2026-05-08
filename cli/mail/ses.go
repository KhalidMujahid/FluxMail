package mail

import (
	"context"
	"fmt"

	"github.com/aws/aws-sdk-go-v2/aws"
	"github.com/aws/aws-sdk-go-v2/credentials"
	"github.com/aws/aws-sdk-go-v2/service/ses"
	"github.com/aws/aws-sdk-go-v2/service/ses/types"

	"github.com/fluxmail/cli/models"
)

type SESSender struct{}

func (s *SESSender) Send(ctx context.Context, p *models.Provider, msg *models.Message) models.SendResult {
	region := p.AwsRegion
	if region == "" {
		region = "us-east-1"
	}

	cfg := aws.Config{
		Region:      region,
		Credentials: credentials.NewStaticCredentialsProvider(p.AwsAccessKeyId, p.AwsSecretAccessKey, ""),
	}
	client := ses.NewFromConfig(cfg)

	body := &types.Body{
		Html: &types.Content{Charset: aws.String("UTF-8"), Data: aws.String(msg.HtmlBody)},
	}
	if msg.PlainTextBody != "" {
		body.Text = &types.Content{Charset: aws.String("UTF-8"), Data: aws.String(msg.PlainTextBody)}
	}

	input := &ses.SendEmailInput{
		Source:      aws.String(effectiveFrom(msg, p)),
		Destination: &types.Destination{ToAddresses: []string{formatAddress(msg.ToName, msg.ToEmail)}},
		Message: &types.Message{
			Subject: &types.Content{Charset: aws.String("UTF-8"), Data: aws.String(msg.Subject)},
			Body:    body,
		},
	}
	if msg.ReplyTo != "" {
		input.ReplyToAddresses = []string{msg.ReplyTo}
	}

	out, err := client.SendEmail(ctx, input)
	if err != nil {
		return models.SendResult{Err: fmt.Errorf("ses: %w", err)}
	}
	return models.SendResult{MessageID: aws.ToString(out.MessageId)}
}
