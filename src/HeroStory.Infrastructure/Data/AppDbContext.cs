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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().HasQueryFilter(user => !user.IsDeleted);
        builder.Entity<StorySession>().HasQueryFilter(session => session.DeletedAt == null);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
