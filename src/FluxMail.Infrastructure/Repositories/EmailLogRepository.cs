using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class EmailLogRepository(AppDbContext db) : IEmailLogRepository
{
    public Task<List<EmailLog>> GetRecentAsync(int count = 50) =>
        db.EmailLogs.OrderByDescending(l => l.SentAt).Take(count).ToListAsync();

    public Task<int> GetSentTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return db.EmailLogs.CountAsync(l => l.Status == EmailStatus.Sent && l.SentAt >= today);
    }

    public Task<int> GetFailedTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return db.EmailLogs.CountAsync(l => l.Status == EmailStatus.Failed && l.SentAt >= today);
    }

    public Task<int> GetTotalSentCountAsync() =>
        db.EmailLogs.CountAsync(l => l.Status == EmailStatus.Sent);

    public Task<int> GetSentTodayByProviderAsync(int providerId)
    {
        var today = DateTime.UtcNow.Date;
        return db.EmailLogs.CountAsync(l =>
            l.ProviderId == providerId && l.Status == EmailStatus.Sent && l.SentAt >= today);
    }

    public async Task AddAsync(EmailLog log)
    {
        db.EmailLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
