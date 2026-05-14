using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using Microsoft.Extensions.Options;

using ResendClient       = Resend.ResendClient;
using ResendClientOpts   = Resend.ResendClientOptions;
using ResendMsg          = Resend.EmailMessage;

namespace FluxMail.Infrastructure.Providers;

public class ResendEmailProvider(IHttpClientFactory httpClientFactory) : IEmailProvider
{
    public ProviderType ProviderType => ProviderType.Resend;

    public async Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        try
        {
            var resend = CreateClient(config);
            var response = await resend.DomainListAsync(ct);
            return response.Success;
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
            var resend = CreateClient(config);
            var senderName = message.FromNameOverride ?? config.SenderName;

            var msg = new ResendMsg();
            msg.From = string.IsNullOrEmpty(senderName)
                ? config.SenderEmail ?? ""
                : $"{senderName} <{config.SenderEmail}>";
            msg.To.Add(string.IsNullOrEmpty(message.ToName)
                ? message.ToEmail
                : $"{message.ToName} <{message.ToEmail}>");
            msg.Subject = message.Subject;
            msg.HtmlBody = message.HtmlBody;

            if (message.PlainTextBody is not null)
                msg.TextBody = message.PlainTextBody;

            if (!string.IsNullOrEmpty(message.ReplyTo))
                msg.ReplyTo = message.ReplyTo;

            var unsubHeader = !string.IsNullOrEmpty(message.UnsubscribeUrl)
                ? $"<{message.UnsubscribeUrl}>, <mailto:{config.SenderEmail}?subject=unsubscribe>"
                : $"<mailto:{config.SenderEmail}?subject=unsubscribe>";
            msg.Headers ??= new Dictionary<string, string>();
            msg.Headers["List-Unsubscribe"] = unsubHeader;
            if (!string.IsNullOrEmpty(message.UnsubscribeUrl))
                msg.Headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";

            var response = await resend.EmailSendAsync(msg, ct);

            if (response.Success)
                return new EmailSendResult(true, response.Content.ToString());

            return new EmailSendResult(false, ErrorMessage: "Resend returned a failure response.");
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    private ResendClient CreateClient(EmailProviderConfig config)
    {
        var opts = new ResendClientOpts { ApiToken = config.ResendApiKey ?? "" };
        var httpClient = httpClientFactory.CreateClient("resend");
        return new ResendClient(new OptionsSnapshot<ResendClientOpts>(opts), httpClient);
    }

    private sealed class OptionsSnapshot<T>(T value) : IOptionsSnapshot<T> where T : class, new()
    {
        public T Value => value;
        public T Get(string? name) => value;
    }
}
