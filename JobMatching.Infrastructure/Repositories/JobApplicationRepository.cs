using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobMatching.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        public JobApplicationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveApplicationAsync(JobApplication jobApplication)
        {
            await _context.JobApplications.AddAsync(jobApplication);
            await _context.SaveChangesAsync();
        }

        public async Task<List<JobApplication>> GetApplicationsByUserIdAsync(string userId)
        {
            return await _context.JobApplications
                .Where(app => app.UserId == userId)
                .Include(app => app.Job)
                .ToListAsync();
        }

        public async Task<JobApplication?> GetApplicationByIdAsync(string applicationId)
        {
            return await _context.JobApplications
                .Include(app => app.Job)
                .FirstOrDefaultAsync(app => app.Id == applicationId);
        }

        public async Task<bool> UpdateApplicationStatusAsync(string applicationId, string status)
        {
            var application = await _context.JobApplications.FindAsync(applicationId);
            if (application == null) return false;

            application.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
