using System.Net;
using System.Text.RegularExpressions;
using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluxMail.Core.Services;

public class EmailService(
    IEmailLogRepository logRepo,
    IEmailQueueRepository queueRepo,
    IProviderRepository providerRepo,
    ProviderRotationService rotationService,
    TrackingService tracking,
    ILogger<EmailService> logger)
{
    public async Task<EmailSendResult> SendAsync(
        int providerId,
        EmailMessage message,
        CancellationToken ct = default)
    {
        var config = await providerRepo.GetByIdAsync(providerId);
        var unsubUrl = BuildUnsubscribeUrl(config, message);

        var trackingId = tracking.GenerateTrackingId();
        message = message with
        {
            UnsubscribeUrl   = unsubUrl,
            HtmlBody         = InjectFooter(tracking.PrepareForSending(message.HtmlBody, trackingId), config, unsubUrl),
            PlainTextBody    = message.PlainTextBody ?? StripHtml(message.HtmlBody)
        };

        var result = await rotationService.SendWithRotationAsync(message, providerId, ct);

        await logRepo.AddAsync(new EmailLog
        {
            ToEmail = message.ToEmail,
            ToName = message.ToName,
            Subject = message.Subject,
            ProviderId = providerId,
            TrackingId = trackingId,
            Status = result.Success ? EmailStatus.Sent : EmailStatus.Failed,
            ErrorMessage = result.ErrorMessage,
            SentAt = DateTime.UtcNow
        });

        if (!result.Success)
            logger.LogWarning("Email to {To} failed: {Error}", message.ToEmail, result.ErrorMessage);

        return result;
    }

    public async Task EnqueueAsync(
        EmailMessage message,
        int? preferredProviderId = null,
        QueuePriority priority = QueuePriority.Normal,
        DateTime? scheduledAt = null,
        int? campaignId = null,
        CancellationToken ct = default)
    {
        await queueRepo.EnqueueAsync(new EmailQueueItem
        {
            ToEmail = message.ToEmail,
            ToName = message.ToName,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            PlainTextBody = message.PlainTextBody,
            PreferredProviderId = preferredProviderId,
            Priority = priority,
            ScheduledAt = scheduledAt,
            CampaignId = campaignId,
            TrackingId = tracking.GenerateTrackingId()
        });
    }

    public Task<bool> TestConnectionAsync(int providerId, CancellationToken ct = default)
        => rotationService.TestConnectionAsync(providerId, ct);

    private static string BuildUnsubscribeUrl(EmailProviderConfig? config, EmailMessage message)
    {
        if (config is null) return "";
        if (!string.IsNullOrEmpty(config.UnsubscribeBaseUrl))
            return $"{config.UnsubscribeBaseUrl}?email={Uri.EscapeDataString(message.ToEmail)}";
        return $"mailto:{config.SenderEmail}?subject=UNSUBSCRIBE%3A{Uri.EscapeDataString(message.ToEmail)}";
    }

    private static readonly Regex ReUnsubHref = new(@"href=""#unsubscribe""", RegexOptions.IgnoreCase);
    private static readonly Regex RePrefHref  = new(@"href=""#preferences""", RegexOptions.IgnoreCase);
    private static readonly Regex ReBodyClose = new(@"</body>", RegexOptions.IgnoreCase);

    private static string InjectFooter(string html, EmailProviderConfig? config, string unsubUrl)
    {
        // Replace placeholder hrefs with real unsubscribe URL
        if (!string.IsNullOrEmpty(unsubUrl))
        {
            html = ReUnsubHref.Replace(html, $"href=\"{unsubUrl}\"");
            html = RePrefHref.Replace(html, $"href=\"{unsubUrl}\"");
        }

        // Inject physical address if configured and not already in the email
        if (config is not null && !string.IsNullOrWhiteSpace(config.PhysicalAddress)
            && !html.Contains(config.PhysicalAddress, StringComparison.OrdinalIgnoreCase))
        {
            var addressBlock = $"""
                <div style="margin-top:24px;text-align:center;font-size:11px;color:#9ca3af;">
                  <p style="margin:0;">{WebUtility.HtmlEncode(config.PhysicalAddress)}</p>
                </div>
                """;
            html = ReBodyClose.IsMatch(html)
                ? ReBodyClose.Replace(html, $"{addressBlock}</body>", 1)
                : html + addressBlock;
        }

        return html;
    }

    private static string StripHtml(string html)
    {
        // Remove script and style blocks entirely
        var text = Regex.Replace(html, @"<(script|style)[^>]*>[\s\S]*?</\1>", "", RegexOptions.IgnoreCase);
        // Block elements become newlines
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|h[1-6]|li|tr|blockquote)>", "\n", RegexOptions.IgnoreCase);
        // Strip remaining tags
        text = Regex.Replace(text, @"<[^>]+>", "");
        // Decode HTML entities (&amp; &nbsp; etc.)
        text = WebUtility.HtmlDecode(text);
        // Normalise whitespace
        text = Regex.Replace(text, @"[ \t]{2,}", " ");
        text = Regex.Replace(text, @"\n[ \t]+", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }
}
