using FluxMail.Core.Interfaces;
using FluxMail.Core.Models;
using FluxMail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxMail.Infrastructure.Repositories;

public class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public Task<UserProfile?> GetAsync()
        => db.UserProfiles.FirstOrDefaultAsync();

    public async Task CreateAsync(UserProfile profile)
    {
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserProfile profile)
    {
        db.UserProfiles.Update(profile);
        await db.SaveChangesAsync();
    }
}
