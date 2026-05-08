using FluxMail.Core.Interfaces;
using FluxMail.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxMail.Infrastructure.BackgroundServices;

public class CampaignScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<CampaignScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Campaign scheduler started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckDueCampaignsAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Campaign scheduler error.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task CheckDueCampaignsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var campaignRepo = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var campaignService = scope.ServiceProvider.GetRequiredService<CampaignService>();

        var due = await campaignRepo.GetDueScheduledAsync();
        if (due.Count == 0) return;

        logger.LogInformation("Launching {Count} scheduled campaign(s).", due.Count);

        foreach (var campaign in due)
        {
            if (ct.IsCancellationRequested) break;
            logger.LogInformation("Starting scheduled campaign: {Name}", campaign.Name);
            _ = Task.Run(() => campaignService.StartAsync(campaign.Id, ct), ct);
        }
    }
}
