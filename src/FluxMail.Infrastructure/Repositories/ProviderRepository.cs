using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class ProviderRepository(AppDbContext db) : IProviderRepository
{
    public Task<List<EmailProviderConfig>> GetAllAsync() =>
        db.Providers.OrderBy(p => p.Name).ToListAsync();

    public Task<EmailProviderConfig?> GetByIdAsync(int id) =>
        db.Providers.FindAsync(id).AsTask();

    public Task<EmailProviderConfig?> GetDefaultAsync() =>
        db.Providers.FirstOrDefaultAsync(p => p.IsDefault);

    public async Task<int> AddAsync(EmailProviderConfig provider)
    {
        db.Providers.Add(provider);
        await db.SaveChangesAsync();
        return provider.Id;
    }

    public async Task UpdateAsync(EmailProviderConfig provider)
    {
        db.Providers.Update(provider);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Providers.FindAsync(id);
        if (entity is not null)
        {
            db.Providers.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task SetDefaultAsync(int id)
    {
        await db.Providers.ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false));
        await db.Providers.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, true));
    }
}
