using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

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
            var senderName = message.FromNameOverride ?? config.SenderName;
            var from = string.IsNullOrEmpty(senderName)
                ? config.SenderEmail ?? ""
                : $"{senderName} <{config.SenderEmail}>";

            var to = string.IsNullOrEmpty(message.ToName)
                ? message.ToEmail
                : $"{message.ToName} <{message.ToEmail}>";

            var body = new Body { Html = new Content { Charset = "UTF-8", Data = message.HtmlBody } };
            if (message.PlainTextBody is not null)
                body.Text = new Content { Charset = "UTF-8", Data = message.PlainTextBody };

            var request = new SendEmailRequest
            {
                Source      = from,
                Destination = new Destination { ToAddresses = [to] },
                Message     = new Message
                {
                    Subject = new Content { Charset = "UTF-8", Data = message.Subject },
                    Body    = body
                }
            };

            if (!string.IsNullOrEmpty(message.ReplyTo))
                request.ReplyToAddresses = [message.ReplyTo];

            using var ses      = CreateClient(config);
            var       response = await ses.SendEmailAsync(request, ct);
            return new EmailSendResult(true, response.MessageId);
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
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
