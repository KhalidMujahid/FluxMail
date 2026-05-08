using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluxMail.Core.Services;

public class EmailService(
    IEmailLogRepository logRepo,
    IEmailQueueRepository queueRepo,
    ProviderRotationService rotationService,
    TrackingService tracking,
    ILogger<EmailService> logger)
{
    public async Task<EmailSendResult> SendAsync(
        int providerId,
        EmailMessage message,
        CancellationToken ct = default)
    {
        var trackingId = tracking.GenerateTrackingId();
        message = message with
        {
            HtmlBody = tracking.PrepareForSending(message.HtmlBody, trackingId)
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
}
