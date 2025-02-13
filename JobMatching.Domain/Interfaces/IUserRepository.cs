using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace JobMatching.Domain.Interfaces;

public interface IUserRepository
{
    Task<IdentityResult> AddAsync(User user, string password);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetUserByIdAsync(string userId);
    Task<IdentityResult> UpdateProfileAsync(User user);
}
