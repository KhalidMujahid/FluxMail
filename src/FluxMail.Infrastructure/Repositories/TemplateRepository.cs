using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class TemplateRepository(AppDbContext db) : ITemplateRepository
{
    public Task<List<EmailTemplate>> GetAllAsync() =>
        db.Templates.OrderByDescending(t => t.UpdatedAt).ToListAsync();

    public Task<EmailTemplate?> GetByIdAsync(int id) =>
        db.Templates.FindAsync(id).AsTask();

    public async Task<int> AddAsync(EmailTemplate template)
    {
        db.Templates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        db.Templates.Update(template);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Templates.FindAsync(id);
        if (entity is not null)
        {
            db.Templates.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
