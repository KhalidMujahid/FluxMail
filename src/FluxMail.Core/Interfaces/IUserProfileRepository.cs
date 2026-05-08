using FluxMail.Core.Models;

namespace FluxMail.Core.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetAsync();
    Task CreateAsync(UserProfile profile);
    Task UpdateAsync(UserProfile profile);
}
