using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public class StorySessionConfiguration : IEntityTypeConfiguration<StorySession>
{
    public void Configure(EntityTypeBuilder<StorySession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(StorySessionFieldLimits.TitleMaxLength).IsRequired();
        builder.Property(x => x.Genre).HasMaxLength(StorySessionFieldLimits.GenreMaxLength).IsRequired();
        builder.Property(x => x.HeroArchetype).HasMaxLength(StorySessionFieldLimits.HeroArchetypeMaxLength).IsRequired();
        builder.Property(x => x.HeroName).HasMaxLength(StorySessionFieldLimits.HeroNameMaxLength).IsRequired();
        builder.Property(x => x.ContinuitySummary).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasMany(x => x.Scenes).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
