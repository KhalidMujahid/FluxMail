using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface IEmailQueueRepository
{
    Task<List<EmailQueueItem>> GetPendingAsync(int batchSize = 20);
    Task<List<EmailQueueItem>> GetAllAsync(int limit = 200);
    Task<EmailQueueItem?> GetByIdAsync(int id);
    Task<int> EnqueueAsync(EmailQueueItem item);
    Task UpdateAsync(EmailQueueItem item);
    Task<int> GetPendingCountAsync();
    Task<int> GetDeadCountAsync();
    Task PurgeDeadAsync();
}
