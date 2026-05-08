using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluxMail.Core.Services;

public class CampaignService(
    ICampaignRepository campaignRepo,
    IContactRepository contactRepo,
    IEmailLogRepository logRepo,
    ProviderRotationService rotationService,
    TrackingService tracking,
    ILogger<CampaignService> logger)
{
    public async Task StartAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await campaignRepo.GetByIdAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.Status == CampaignStatus.Sending) return;

        var contacts = campaign.ContactListId.HasValue
            ? await contactRepo.GetByListIdAsync(campaign.ContactListId.Value)
            : await contactRepo.GetAllAsync();

        campaign.Status = CampaignStatus.Sending;
        campaign.StartedAt = DateTime.UtcNow;
        campaign.TotalRecipients = contacts.Count;
        campaign.SentCount = 0;
        campaign.FailedCount = 0;
        await campaignRepo.UpdateAsync(campaign);

        foreach (var contact in contacts)
        {
            if (ct.IsCancellationRequested) break;

            var trackingId = tracking.GenerateTrackingId();
            var body = tracking.PrepareForSending(
                ResolveVariables(campaign.HtmlBody, contact), trackingId);
            var subject = ResolveVariables(campaign.Subject, contact);
            var plain = campaign.PlainTextBody is not null
                ? ResolveVariables(campaign.PlainTextBody, contact)
                : null;

            var message = new EmailMessage
            {
                ToEmail = contact.Email,
                ToName = contact.Name,
                Subject = subject,
                HtmlBody = body,
                PlainTextBody = plain,
                FromNameOverride = string.IsNullOrWhiteSpace(campaign.FromNameOverride) ? null : campaign.FromNameOverride
            };

            var result = await rotationService.SendWithRotationAsync(message, campaign.ProviderId, ct);

            var log = new EmailLog
            {
                ToEmail = contact.Email,
                ToName = contact.Name,
                Subject = subject,
                CampaignId = campaignId,
                ProviderId = campaign.ProviderId,
                TrackingId = trackingId,
                Status = result.Success ? EmailStatus.Sent : EmailStatus.Failed,
                ErrorMessage = result.ErrorMessage,
                SentAt = DateTime.UtcNow
            };

            await campaignRepo.AddLogAsync(log);
            await logRepo.AddAsync(log);

            if (result.Success)
                campaign.SentCount++;
            else
            {
                campaign.FailedCount++;
                logger.LogWarning("Failed sending to {Email}: {Error}", contact.Email, result.ErrorMessage);
            }

            await campaignRepo.UpdateAsync(campaign);
            await Task.Delay(50, ct);
        }

        campaign.Status = ct.IsCancellationRequested ? CampaignStatus.Failed : CampaignStatus.Completed;
        campaign.CompletedAt = DateTime.UtcNow;

        if (!ct.IsCancellationRequested && campaign.Recurrence != RecurrenceType.None)
        {
            campaign.NextRunAt = ComputeNextRun(campaign.Recurrence, campaign.RecurrenceInterval);
            campaign.Status = CampaignStatus.Scheduled;
        }

        await campaignRepo.UpdateAsync(campaign);
    }

    private static DateTime ComputeNextRun(RecurrenceType type, int interval) => type switch
    {
        RecurrenceType.Daily => DateTime.UtcNow.AddDays(interval),
        RecurrenceType.Weekly => DateTime.UtcNow.AddDays(interval * 7),
        RecurrenceType.Monthly => DateTime.UtcNow.AddMonths(interval),
        _ => DateTime.UtcNow.AddDays(1)
    };

    private static string ResolveVariables(string template, Contact c) =>
        template
            .Replace("{{name}}", c.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{{email}}", c.Email, StringComparison.OrdinalIgnoreCase)
            .Replace("{{company}}", c.Company ?? "", StringComparison.OrdinalIgnoreCase);
}
