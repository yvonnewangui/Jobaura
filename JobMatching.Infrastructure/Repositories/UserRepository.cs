using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobMatching.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public UserRepository(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// 🔥 Add a new user to the Identity system.
    /// </summary>
    public async Task<IdentityResult> AddAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    /// <summary>
    /// 🔥 Delete a user by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }
        return false;
    }

    /// <summary>
    /// 🔥 Get all users with profile data.
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.JobApplications) // Include applications
            .ToListAsync();
    }

    /// <summary>
    /// 🔥 Get a user by ID (Includes profile details).
    /// </summary>
    public async Task<User?> GetByIdAsync(string id)
    {
        return await _context.Users
            .Include(u => u.JobApplications)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// 🔥 Get user by ID with IdentityUser support.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.Users
            .Include(u => u.JobApplications)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// 🔥 Update user profile (excluding sensitive identity fields).
    /// </summary>
    public async Task<IdentityResult> UpdateProfileAsync(User user)
    {
        var existingUser = await _userManager.FindByIdAsync(user.Id);
        if (existingUser == null) return IdentityResult.Failed();

        existingUser.FullName = user.FullName;
        existingUser.ResumeUrl = user.ResumeUrl;
        existingUser.SubscriptionTier = user.SubscriptionTier;
        existingUser.SubscriptionExpiry = user.SubscriptionExpiry;

        return await _userManager.UpdateAsync(existingUser);
    }
}
