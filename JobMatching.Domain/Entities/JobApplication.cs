using System;

namespace JobMatching.Domain.Entities
{
    public class JobApplication
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();  // Unique Application ID
        public required string UserId { get; set; }  // The ID of the Job Seeker
        public User User { get; set; }  // Navigation Property for User
        public required string JobId { get; set; }  // The ID of the Job Being Applied For
        public Job Job { get; set; }  // Navigation Property for Job
        public string ResumeText { get; set; } = string.Empty;  // AI-Optimized Resume
        public string CoverLetter { get; set; } = string.Empty;  // AI-Generated Cover Letter
        public string Status { get; set; } = "Pending";  // Application Status: Pending, Accepted, Rejected
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;  // Application Date
    }
}
