using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using MimeKit;

namespace FluxMail.Infrastructure.Providers;

public class SesEmailProvider : IEmailProvider
{
    public ProviderType ProviderType => ProviderType.AwsSes;

    public async Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        try
        {
            using var ses = CreateClient(config);
            var response = await ses.ListIdentitiesAsync(new ListIdentitiesRequest { MaxItems = 1 }, ct);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch { return false; }
    }

    public async Task<EmailSendResult> SendAsync(EmailProviderConfig config, EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var mime = BuildMimeMessage(config, message);

            using var memStream = new MemoryStream();
            await mime.WriteToAsync(memStream, ct);
            memStream.Position = 0;

            var request = new SendRawEmailRequest
            {
                RawMessage = new RawMessage { Data = memStream }
            };

            using var ses = CreateClient(config);
            var response = await ses.SendRawEmailAsync(request, ct);
            return new EmailSendResult(true, response.MessageId);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    private static MimeMessage BuildMimeMessage(EmailProviderConfig config, EmailMessage message)
    {
        var mime = new MimeMessage();

        var senderName = message.FromNameOverride ?? config.SenderName;
        mime.From.Add(new MailboxAddress(senderName, config.SenderEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;

        if (!string.IsNullOrEmpty(message.ReplyTo))
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

        var unsubHeader = !string.IsNullOrEmpty(message.UnsubscribeUrl)
            ? $"<{message.UnsubscribeUrl}>, <mailto:{config.SenderEmail}?subject=unsubscribe>"
            : $"<mailto:{config.SenderEmail}?subject=unsubscribe>";
        mime.Headers.Add("List-Unsubscribe", unsubHeader);
        if (!string.IsNullOrEmpty(message.UnsubscribeUrl))
            mime.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        mime.Body = builder.ToMessageBody();
        return mime;
    }

    private static AmazonSimpleEmailServiceClient CreateClient(EmailProviderConfig config)
    {
        var credentials = new BasicAWSCredentials(
            config.AwsAccessKeyId     ?? "",
            config.AwsSecretAccessKey ?? "");
        var region = RegionEndpoint.GetBySystemName(config.AwsRegion ?? "us-east-1");
        return new AmazonSimpleEmailServiceClient(credentials, region);
    }
}
