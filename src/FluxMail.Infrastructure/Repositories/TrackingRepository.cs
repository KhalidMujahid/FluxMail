using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class TrackingRepository(AppDbContext db) : ITrackingRepository
{
    public async Task RecordEventAsync(EmailTrackingEvent evt)
    {
        db.TrackingEvents.Add(evt);
        await db.SaveChangesAsync();
    }

    public Task<List<EmailTrackingEvent>> GetByTrackingIdAsync(string trackingId) =>
        db.TrackingEvents.Where(e => e.TrackingId == trackingId).ToListAsync();

    public Task<List<EmailTrackingEvent>> GetByCampaignAsync(int campaignId) =>
        db.TrackingEvents.Where(e => e.CampaignId == campaignId).ToListAsync();

    public async Task<(int Opens, int Clicks)> GetCampaignStatsAsync(int campaignId)
    {
        var opens = await db.TrackingEvents.CountAsync(e =>
            e.CampaignId == campaignId && e.EventType == TrackingEventType.Open);
        var clicks = await db.TrackingEvents.CountAsync(e =>
            e.CampaignId == campaignId && e.EventType == TrackingEventType.Click);
        return (opens, clicks);
    }

    public async Task IncrementOpenAsync(string trackingId)
    {
        var log = await db.EmailLogs.FirstOrDefaultAsync(l => l.TrackingId == trackingId);
        if (log is not null) { log.OpenCount++; await db.SaveChangesAsync(); }

        await RecordEventAsync(new EmailTrackingEvent
        {
            TrackingId = trackingId,
            EventType = TrackingEventType.Open,
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task IncrementClickAsync(string trackingId)
    {
        var log = await db.EmailLogs.FirstOrDefaultAsync(l => l.TrackingId == trackingId);
        if (log is not null) { log.ClickCount++; await db.SaveChangesAsync(); }

        await RecordEventAsync(new EmailTrackingEvent
        {
            TrackingId = trackingId,
            EventType = TrackingEventType.Click,
            OccurredAt = DateTime.UtcNow
        });
    }
}
