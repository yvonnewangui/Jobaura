using Microsoft.EntityFrameworkCore;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

public class JobRepository : Repository<Job>, IJobRepository
{
    public JobRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Job>> GetJobsByRecruiterIdAsync(int recruiterId)
    {
        return await _context.Jobs.Where(j => j.PostedBy == recruiterId).ToListAsync();
    }
}
