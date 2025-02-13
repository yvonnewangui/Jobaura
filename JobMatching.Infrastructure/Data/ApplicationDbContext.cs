using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using JobMatching.Domain.Entities;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<PaymentRecord> PaymentRecords { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasMany(u => u.Jobs).WithOne(j => j.Recruiter).HasForeignKey(j => j.PostedBy);

        modelBuilder.Entity<JobApplication>()
               .HasOne(ja => ja.User)
               .WithMany()
               .HasForeignKey(ja => ja.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobApplication>()
            .HasOne(ja => ja.Job)
            .WithMany()
            .HasForeignKey(ja => ja.JobId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
