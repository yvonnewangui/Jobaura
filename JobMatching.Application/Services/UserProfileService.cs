using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public class UserProfileService
{
    private readonly UserManager<User> _userManager;

    public UserProfileService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User?> GetUserProfileAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<bool> UpdateUserProfileAsync(string userId, User updatedUser)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.FullName = updatedUser.FullName;
        user.ProfilePictureUrl = updatedUser.ProfilePictureUrl;
        user.LinkedInProfile = updatedUser.LinkedInProfile;
        user.ResumeUrl = updatedUser.ResumeUrl;
        user.Skills = updatedUser.Skills;
        user.Experience = updatedUser.Experience;
        user.JobPreferences = updatedUser.JobPreferences;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
