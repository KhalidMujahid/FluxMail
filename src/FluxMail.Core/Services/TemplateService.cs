using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;

namespace FluxMail.Core.Services;

public class TemplateService(ITemplateRepository repo)
{
    public Task<List<EmailTemplate>> GetAllAsync() => repo.GetAllAsync();
    public Task<EmailTemplate?> GetByIdAsync(int id) => repo.GetByIdAsync(id);

    public async Task<int> CreateAsync(EmailTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        return await repo.AddAsync(template);
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(template);
    }

    public Task DeleteAsync(int id) => repo.DeleteAsync(id);

    public string ApplyToContact(EmailTemplate template, Models.Contact contact)
    {
        return template.HtmlBody
            .Replace("{{name}}", contact.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{{email}}", contact.Email, StringComparison.OrdinalIgnoreCase)
            .Replace("{{company}}", contact.Company ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
