using Microsoft.EntityFrameworkCore;

using JobMagnet.Data.Entities;

namespace JobMagnet.Data;

public class JobMagnetDbContext(DbContextOptions<JobMagnetDbContext> options) : DbContext(options)
{
    private const int EmailMaxLength = 320;
    private const int SourceMaxLength = 64;
    private const int HashLength = 64;

    public DbSet<User> Users => Set<User>();

    public DbSet<CvDocument> CvDocuments => Set<CvDocument>();

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(EmailMaxLength);
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Profile>(profile =>
        {
            profile.Property(p => p.Data).HasColumnType("jsonb");
            profile.HasIndex(p => p.UserId).IsUnique();
        });

        modelBuilder.Entity<Job>(job =>
        {
            job.Property(j => j.Source).HasMaxLength(SourceMaxLength);
            job.Property(j => j.DedupeHash).HasMaxLength(HashLength);
            job.HasIndex(j => new { j.Source, j.ExternalId }).IsUnique();
            job.HasIndex(j => j.DedupeHash);
            job.HasIndex(j => j.IsActive);
            job.HasIndex(j => j.SubmittedByUserId);
            job.HasIndex(j => new { j.Source, j.FirstSeenAt });
        });

        modelBuilder.Entity<Match>(match =>
        {
            match.HasIndex(m => new { m.UserId, m.JobId }).IsUnique();
            match.Property(m => m.Status).HasConversion<string>();
        });
    }
}
