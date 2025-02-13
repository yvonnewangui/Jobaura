using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

public interface IJobRepository : IRepository<Job>
{
    Task<IEnumerable<Job>> GetJobsByRecruiterIdAsync(int recruiterId);
}
