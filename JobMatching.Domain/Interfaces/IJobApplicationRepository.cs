using System.Collections.Generic;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;

namespace JobMatching.Domain.Interfaces
{
    public interface IJobApplicationRepository
    {
        Task SaveApplicationAsync(JobApplication jobApplication);
        Task<List<JobApplication>> GetApplicationsByUserIdAsync(string userId);
        Task<JobApplication?> GetApplicationByIdAsync(string applicationId);
        Task<bool> UpdateApplicationStatusAsync(string applicationId, string status);
    }
}
