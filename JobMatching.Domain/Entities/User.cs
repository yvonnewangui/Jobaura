using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace JobMatching.Domain.Entities;
public class User : IdentityUser
{
    public string Role { get; set; } = "JobSeeker";  
    public string FullName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string LinkedInProfile { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
    public string OptimizedResumeUrl { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string Experience { get; set; } = string.Empty;
    public string JobPreferences { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";  // Free, Premium, Pro
    public DateTime? SubscriptionExpiry { get; set; }
    public List<PaymentRecord> PaymentRecords { get; set; } = new();
    public List<Job> Jobs { get; set; } = new();
    public List<JobApplication> JobApplications { get; set; } = new();
}
