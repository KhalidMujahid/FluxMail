using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface IProviderRepository
{
    Task<List<EmailProviderConfig>> GetAllAsync();
    Task<EmailProviderConfig?> GetByIdAsync(int id);
    Task<EmailProviderConfig?> GetDefaultAsync();
    Task<int> AddAsync(EmailProviderConfig provider);
    Task UpdateAsync(EmailProviderConfig provider);
    Task DeleteAsync(int id);
    Task SetDefaultAsync(int id);
}
