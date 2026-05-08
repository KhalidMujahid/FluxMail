using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface ICampaignRepository
{
    Task<List<Campaign>> GetAllAsync();
    Task<Campaign?> GetByIdAsync(int id);
    Task<List<Campaign>> GetDueScheduledAsync();
    Task<int> AddAsync(Campaign campaign);
    Task UpdateAsync(Campaign campaign);
    Task DeleteAsync(int id);
    Task AddLogAsync(EmailLog log);
    Task<List<EmailLog>> GetLogsAsync(int campaignId);
}
