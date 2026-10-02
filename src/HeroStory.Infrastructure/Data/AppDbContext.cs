using HeroStory.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<StorySession> StorySessions => Set<StorySession>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeletionAuditLog> DeletionAuditLogs => Set<DeletionAuditLog>();
    public DbSet<UserPortrait> UserPortraits => Set<UserPortrait>();
    public DbSet<PortraitConsentRecord> PortraitConsentRecords => Set<PortraitConsentRecord>();
    public DbSet<PortraitAuditEvent> PortraitAuditEvents => Set<PortraitAuditEvent>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsurePortraitRecordsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsurePortraitRecordsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().HasQueryFilter(user => !user.IsDeleted);
        builder.Entity<StorySession>().HasQueryFilter(session => session.DeletedAt == null);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private void EnsurePortraitRecordsAreAppendOnly()
    {
        if (ChangeTracker.Entries().Any(entry =>
                (entry.Entity is PortraitConsentRecord or PortraitAuditEvent)
                && entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Portrait consent records and audit events are append-only.");
        }
    }
}
