using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Infrastructure.Providers;

public class MailgunEmailProvider(IHttpClientFactory httpClientFactory) : IEmailProvider
{
    public ProviderType ProviderType => ProviderType.Mailgun;

    public async Task<bool> TestConnectionAsync(EmailProviderConfig config, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient(config);
            var response = await client.GetAsync($"https://api.mailgun.net/v3/domains/{config.MailgunDomain}", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<EmailSendResult> SendAsync(EmailProviderConfig config, EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient(config);

            var senderName = message.FromNameOverride ?? config.SenderName;
            var from = string.IsNullOrEmpty(senderName)
                ? config.SenderEmail
                : $"{senderName} <{config.SenderEmail}>";

            var to = string.IsNullOrEmpty(message.ToName)
                ? message.ToEmail
                : $"{message.ToName} <{message.ToEmail}>";

            var form = new MultipartFormDataContent
            {
                { new StringContent(from), "from" },
                { new StringContent(to), "to" },
                { new StringContent(message.Subject), "subject" },
                { new StringContent(message.HtmlBody), "html" }
            };

            if (message.PlainTextBody is not null)
                form.Add(new StringContent(message.PlainTextBody), "text");

            var unsubHeader = !string.IsNullOrEmpty(message.UnsubscribeUrl)
                ? $"<{message.UnsubscribeUrl}>, <mailto:{config.SenderEmail}?subject=unsubscribe>"
                : $"<mailto:{config.SenderEmail}?subject=unsubscribe>";
            form.Add(new StringContent(unsubHeader), "h:List-Unsubscribe");
            if (!string.IsNullOrEmpty(message.UnsubscribeUrl))
                form.Add(new StringContent("List-Unsubscribe=One-Click"), "h:List-Unsubscribe-Post");

            var response = await client.PostAsync(
                $"https://api.mailgun.net/v3/{config.MailgunDomain}/messages", form, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<MailgunResponse>(cancellationToken: ct);
                return new EmailSendResult(true, body?.Id);
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new EmailSendResult(false, ErrorMessage: $"HTTP {(int)response.StatusCode}: {errorBody}");
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    private HttpClient CreateClient(EmailProviderConfig config)
    {
        var client = httpClientFactory.CreateClient("mailgun");
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{config.MailgunApiKey}"));
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }

    private sealed record MailgunResponse(string Id, string Message);
}
