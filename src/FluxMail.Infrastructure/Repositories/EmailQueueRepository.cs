using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class EmailQueueRepository(AppDbContext db) : IEmailQueueRepository
{
    public Task<List<EmailQueueItem>> GetPendingAsync(int batchSize = 20)
    {
        var now = DateTime.UtcNow;
        return db.Queue
            .Where(q => q.Status == QueueItemStatus.Pending &&
                        (q.ScheduledAt == null || q.ScheduledAt <= now))
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public Task<List<EmailQueueItem>> GetAllAsync(int limit = 200) =>
        db.Queue.OrderByDescending(q => q.CreatedAt).Take(limit).ToListAsync();

    public Task<EmailQueueItem?> GetByIdAsync(int id) =>
        db.Queue.FindAsync(id).AsTask();

    public async Task<int> EnqueueAsync(EmailQueueItem item)
    {
        db.Queue.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateAsync(EmailQueueItem item)
    {
        db.Queue.Update(item);
        await db.SaveChangesAsync();
    }

    public Task<int> GetPendingCountAsync() =>
        db.Queue.CountAsync(q => q.Status == QueueItemStatus.Pending);

    public Task<int> GetDeadCountAsync() =>
        db.Queue.CountAsync(q => q.Status == QueueItemStatus.Dead);

    public async Task PurgeDeadAsync()
    {
        var dead = await db.Queue.Where(q => q.Status == QueueItemStatus.Dead).ToListAsync();
        db.Queue.RemoveRange(dead);
        await db.SaveChangesAsync();
    }
}
