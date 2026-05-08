using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class CampaignRepository(AppDbContext db) : ICampaignRepository
{
    public Task<List<Campaign>> GetAllAsync() =>
        db.Campaigns.OrderByDescending(c => c.CreatedAt).ToListAsync();

    public Task<Campaign?> GetByIdAsync(int id) =>
        db.Campaigns
            .Include(c => c.ContactList)
            .Include(c => c.Provider)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<List<Campaign>> GetDueScheduledAsync()
    {
        var now = DateTime.UtcNow;
        return db.Campaigns
            .Where(c => c.Status == CampaignStatus.Scheduled &&
                        c.NextRunAt.HasValue && c.NextRunAt.Value <= now)
            .ToListAsync();
    }

    public async Task<int> AddAsync(Campaign campaign)
    {
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        return campaign.Id;
    }

    public async Task UpdateAsync(Campaign campaign)
    {
        db.Campaigns.Update(campaign);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Campaigns.FindAsync(id);
        if (entity is not null) { db.Campaigns.Remove(entity); await db.SaveChangesAsync(); }
    }

    public async Task AddLogAsync(EmailLog log)
    {
        db.EmailLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public Task<List<EmailLog>> GetLogsAsync(int campaignId) =>
        db.EmailLogs.Where(l => l.CampaignId == campaignId)
            .OrderByDescending(l => l.SentAt).ToListAsync();
}
