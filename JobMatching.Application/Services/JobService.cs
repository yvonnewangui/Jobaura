using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

namespace JobMatching.Application.Services;
public class JobService
{
    private readonly IJobRepository _jobRepository;
    public JobService(IJobRepository jobRepository) => _jobRepository = jobRepository;

    public async Task<IEnumerable<Job>> GetJobsAsync() => await _jobRepository.GetAllAsync();
    public async Task AddJobAsync(Job job) => await _jobRepository.AddAsync(job);
}
