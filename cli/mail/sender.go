package mail

import (
	"context"
	"fmt"
	"html"
	"regexp"
	"strings"

	"github.com/fluxmail/cli/models"
)

var (
	reScriptStyle = regexp.MustCompile(`(?is)<(script|style)[^>]*>.*?</(script|style)>`)
	reBr          = regexp.MustCompile(`(?i)<br\s*/?>`)
	reBlock       = regexp.MustCompile(`(?i)</(p|div|h[1-6]|li|tr|blockquote)>`)
	reTags        = regexp.MustCompile(`<[^>]+>`)
	reSpaces      = regexp.MustCompile(`[ \t]{2,}`)
	reBlankLines  = regexp.MustCompile(`\n{3,}`)
)

func stripHTML(s string) string {
	s = reScriptStyle.ReplaceAllString(s, "")
	s = reBr.ReplaceAllString(s, "\n")
	s = reBlock.ReplaceAllString(s, "\n")
	s = reTags.ReplaceAllString(s, "")
	s = html.UnescapeString(s)
	s = reSpaces.ReplaceAllString(s, " ")
	s = reBlankLines.ReplaceAllString(s, "\n\n")
	return strings.TrimSpace(s)
}

func effectivePlainText(msg *models.Message) string {
	if msg.PlainTextBody != "" {
		return msg.PlainTextBody
	}
	return stripHTML(msg.HtmlBody)
}

func unsubscribeHeader(msg *models.Message, p *models.Provider) string {
	if msg.UnsubscribeUrl != "" {
		return fmt.Sprintf("<%s>, <mailto:%s?subject=unsubscribe>", msg.UnsubscribeUrl, p.SenderEmail)
	}
	return fmt.Sprintf("<mailto:%s?subject=unsubscribe>", p.SenderEmail)
}

// PrepareMessage sets UnsubscribeUrl, replaces placeholder hrefs, and injects
// the physical address footer. Call this before passing the message to a sender.
func PrepareMessage(msg *models.Message, p *models.Provider) *models.Message {
	// Build unsubscribe URL
	if msg.UnsubscribeUrl == "" {
		if p.UnsubscribeBaseUrl != "" {
			msg.UnsubscribeUrl = p.UnsubscribeBaseUrl + "?email=" + msg.ToEmail
		} else {
			msg.UnsubscribeUrl = fmt.Sprintf("mailto:%s?subject=UNSUBSCRIBE%%3A%s", p.SenderEmail, msg.ToEmail)
		}
	}

	html := msg.HtmlBody

	// Replace placeholder hrefs
	html = reUnsubHref.ReplaceAllString(html, fmt.Sprintf(`href="%s"`, msg.UnsubscribeUrl))
	html = rePrefHref.ReplaceAllString(html, fmt.Sprintf(`href="%s"`, msg.UnsubscribeUrl))

	// Inject physical address if set and not already present
	if p.PhysicalAddress != "" && !strings.Contains(html, p.PhysicalAddress) {
		addressBlock := fmt.Sprintf(
			`<div style="margin-top:24px;text-align:center;font-size:11px;color:#9ca3af;"><p style="margin:0;">%s</p></div>`,
			p.PhysicalAddress)
		if reBodyClose.MatchString(html) {
			html = reBodyClose.ReplaceAllString(html, addressBlock+"</body>")
		} else {
			html += addressBlock
		}
	}

	// Auto plain-text
	plainText := effectivePlainText(msg)

	return &models.Message{
		ToEmail:          msg.ToEmail,
		ToName:           msg.ToName,
		Subject:          msg.Subject,
		HtmlBody:         html,
		PlainTextBody:    plainText,
		ReplyTo:          msg.ReplyTo,
		FromNameOverride: msg.FromNameOverride,
		UnsubscribeUrl:   msg.UnsubscribeUrl,
	}
}

var (
	reUnsubHref = regexp.MustCompile(`(?i)href="#unsubscribe"`)
	rePrefHref  = regexp.MustCompile(`(?i)href="#preferences"`)
	reBodyClose = regexp.MustCompile(`(?i)</body>`)
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
