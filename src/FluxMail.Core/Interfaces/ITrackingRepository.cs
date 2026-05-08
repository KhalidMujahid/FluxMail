using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface ITrackingRepository
{
    Task RecordEventAsync(EmailTrackingEvent evt);
    Task<List<EmailTrackingEvent>> GetByTrackingIdAsync(string trackingId);
    Task<List<EmailTrackingEvent>> GetByCampaignAsync(int campaignId);
    Task<(int Opens, int Clicks)> GetCampaignStatsAsync(int campaignId);
    Task IncrementOpenAsync(string trackingId);
    Task IncrementClickAsync(string trackingId);
}
