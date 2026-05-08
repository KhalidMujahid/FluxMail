using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface ITemplateRepository
{
    Task<List<EmailTemplate>> GetAllAsync();
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task<int> AddAsync(EmailTemplate template);
    Task UpdateAsync(EmailTemplate template);
    Task DeleteAsync(int id);
}
