using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluxMail.Core.Services;

public class RotationState
{
    private int _index;
    public int Next(int count) => count == 0 ? 0 : Math.Abs(Interlocked.Increment(ref _index) % count);
}

public class ProviderRotationService(
    IProviderRepository providerRepo,
    IEmailLogRepository logRepo,
    IEnumerable<IEmailProvider> providers,
    ProviderRateLimiter rateLimiter,
    RotationState state,
    ILogger<ProviderRotationService> logger)
{
    public async Task<EmailSendResult> SendWithRotationAsync(
        EmailMessage message,
        int? preferredProviderId = null,
        CancellationToken ct = default)
    {
        var all = await providerRepo.GetAllAsync();
        var active = all.Where(p => p.IsEnabled).ToList();

        if (active.Count == 0)
            return new EmailSendResult(false, ErrorMessage: "No active providers configured.");

        var ordered = BuildTryOrder(active, preferredProviderId, state);
        var lastError = "All providers failed.";

        foreach (var config in ordered)
        {
            if (config.DailySendingLimit.HasValue)
            {
                var sentToday = await logRepo.GetSentTodayByProviderAsync(config.Id);
                if (sentToday >= config.DailySendingLimit.Value)
                {
                    logger.LogDebug("Provider {Name} daily limit reached ({Limit})", config.Name, config.DailySendingLimit);
                    continue;
                }
            }

            if (!rateLimiter.TryAcquire(config.Id, config.SendsPerMinute))
            {
                logger.LogDebug("Provider {Name} rate limit reached", config.Name);
                continue;
            }

            var handler = providers.FirstOrDefault(p => p.ProviderType == config.Type);
            if (handler is null)
            {
                logger.LogWarning("No handler registered for provider type {Type}", config.Type);
                continue;
            }

            var result = await handler.SendAsync(config, message, ct);
            if (result.Success)
            {
                logger.LogDebug("Sent via {Provider}", config.Name);
                return result;
            }

            lastError = result.ErrorMessage ?? lastError;
            logger.LogWarning("Provider {Name} failed: {Error}", config.Name, lastError);
        }

        return new EmailSendResult(false, ErrorMessage: lastError);
    }

    public async Task<bool> TestConnectionAsync(int providerId, CancellationToken ct = default)
    {
        var config = await providerRepo.GetByIdAsync(providerId) ?? throw new InvalidOperationException("Provider not found.");
        var handler = providers.FirstOrDefault(p => p.ProviderType == config.Type) ?? throw new InvalidOperationException("No handler for provider type.");
        return await handler.TestConnectionAsync(config, ct);
    }

    private static List<EmailProviderConfig> BuildTryOrder(
        List<EmailProviderConfig> active,
        int? preferredId,
        RotationState rotationState)
    {
        var result = new List<EmailProviderConfig>();

        if (preferredId.HasValue)
        {
            var preferred = active.FirstOrDefault(p => p.Id == preferredId.Value);
            if (preferred is not null) result.Add(preferred);
        }

        var def = active.FirstOrDefault(p => p.IsDefault);
        if (def is not null && !result.Contains(def)) result.Add(def);

        var remaining = active.Except(result).ToList();
        var expanded = remaining.SelectMany(p => Enumerable.Repeat(p, p.ProviderWeight)).ToList();
        if (expanded.Count > 0)
        {
            var startIdx = rotationState.Next(expanded.Count);
            result.AddRange(expanded.Skip(startIdx).Concat(expanded.Take(startIdx)).Distinct());
        }

        return result;
    }
}
