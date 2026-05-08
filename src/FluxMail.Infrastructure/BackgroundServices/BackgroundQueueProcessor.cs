using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxMail.Infrastructure.BackgroundServices;

public class BackgroundQueueProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundQueueProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Queue processor started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queue processor error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var queueRepo = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var rotation = scope.ServiceProvider.GetRequiredService<ProviderRotationService>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IEmailLogRepository>();

        var items = await queueRepo.GetPendingAsync(batchSize: 10);
        if (items.Count == 0) return;

        logger.LogDebug("Processing {Count} queue items.", items.Count);

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested) break;

            item.Status = QueueItemStatus.Processing;
            await queueRepo.UpdateAsync(item);

            var message = new EmailMessage
            {
                ToEmail = item.ToEmail,
                ToName = item.ToName,
                Subject = item.Subject,
                HtmlBody = item.HtmlBody,
                PlainTextBody = item.PlainTextBody
            };

            var result = await rotation.SendWithRotationAsync(message, item.PreferredProviderId, ct);

            if (result.Success)
            {
                item.Status = QueueItemStatus.Sent;
                item.ProcessedAt = DateTime.UtcNow;

                await logRepo.AddAsync(new EmailLog
                {
                    ToEmail = item.ToEmail,
                    ToName = item.ToName,
                    Subject = item.Subject,
                    CampaignId = item.CampaignId,
                    TrackingId = item.TrackingId,
                    Status = EmailStatus.Sent,
                    SentAt = DateTime.UtcNow
                });
            }
            else
            {
                item.RetryCount++;
                item.LastError = result.ErrorMessage;

                if (item.RetryCount >= item.MaxRetries)
                {
                    item.Status = QueueItemStatus.Dead;
                    logger.LogWarning("Item {Id} to {Email} moved to dead queue after {Retries} retries.",
                        item.Id, item.ToEmail, item.RetryCount);
                }
                else
                {
                    // Exponential backoff: 5min, 15min, 45min
                    var delayMinutes = Math.Pow(3, item.RetryCount) * 5;
                    item.Status = QueueItemStatus.Pending;
                    item.ScheduledAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                    logger.LogDebug("Item {Id} retry {N} scheduled in {Min}min.", item.Id, item.RetryCount, delayMinutes);
                }
            }

            await queueRepo.UpdateAsync(item);
        }
    }
}
