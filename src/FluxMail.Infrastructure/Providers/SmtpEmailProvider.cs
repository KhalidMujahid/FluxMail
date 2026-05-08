using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FluxMail.Infrastructure.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    public ProviderType ProviderType => ProviderType.Smtp;

    public async Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost ?? "", config.SmtpPort,
                config.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            if (!string.IsNullOrEmpty(config.SmtpUsername))
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword ?? "", ct);
            await client.DisconnectAsync(true, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EmailSendResult> SendAsync(EmailProviderConfig config, EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var mime = BuildMimeMessage(config, message);

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost ?? "", config.SmtpPort,
                config.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

            if (!string.IsNullOrEmpty(config.SmtpUsername))
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword ?? "", ct);

            var messageId = await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            return new EmailSendResult(true, messageId);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    private static MimeMessage BuildMimeMessage(EmailProviderConfig config, EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(message.FromNameOverride ?? config.SenderName, config.SenderEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;

        if (!string.IsNullOrEmpty(message.ReplyTo))
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        mime.Body = builder.ToMessageBody();
        return mime;
    }
}
