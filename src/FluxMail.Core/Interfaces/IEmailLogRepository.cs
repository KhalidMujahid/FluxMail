using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface IEmailLogRepository
{
    Task<List<EmailLog>> GetRecentAsync(int count = 50);
    Task<int> GetSentTodayCountAsync();
    Task<int> GetFailedTodayCountAsync();
    Task<int> GetTotalSentCountAsync();
    Task<int> GetSentTodayByProviderAsync(int providerId);
    Task AddAsync(EmailLog log);
}
