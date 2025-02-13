namespace JobMatching.Domain.Entities;
public class Job
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> SkillsRequired { get; set; } = new();
    public int PostedBy { get; set; }
    public required User Recruiter { get; set; }
    public List<JobApplication> JobApplications { get; set; } = new();
}
