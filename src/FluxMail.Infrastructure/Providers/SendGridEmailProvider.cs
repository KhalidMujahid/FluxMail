using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FluxMail.Infrastructure.Providers;

public class SendGridEmailProvider : IEmailProvider
{
    public ProviderType ProviderType => ProviderType.SendGrid;

    public async Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        try
        {
            var client = new SendGridClient(config.SendGridApiKey ?? "");
            var response = await client.RequestAsync(
                SendGridClient.Method.GET,
                urlPath: "user/profile",
                cancellationToken: ct);
            return (int)response.StatusCode is >= 200 and < 300;
        }
        catch { return false; }
    }

    public async Task<EmailSendResult> SendAsync(EmailProviderConfig config, EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var senderName = message.FromNameOverride ?? config.SenderName;
            var from = new EmailAddress(config.SenderEmail ?? "", senderName);
            var to   = new EmailAddress(message.ToEmail, string.IsNullOrEmpty(message.ToName) ? null : message.ToName);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                message.Subject,
                message.PlainTextBody,
                message.HtmlBody);

            if (!string.IsNullOrEmpty(message.ReplyTo))
                msg.ReplyTo = new EmailAddress(message.ReplyTo);

            var client   = new SendGridClient(config.SendGridApiKey ?? "");
            var response = await client.SendEmailAsync(msg, ct);

            if ((int)response.StatusCode is >= 200 and < 300)
            {
                var msgId = response.Headers.TryGetValues("X-Message-Id", out var vals)
                    ? vals.FirstOrDefault()
                    : null;
                return new EmailSendResult(true, msgId);
            }

            var body = await response.Body.ReadAsStringAsync(ct);
            return new EmailSendResult(false, ErrorMessage: $"HTTP {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }
}
