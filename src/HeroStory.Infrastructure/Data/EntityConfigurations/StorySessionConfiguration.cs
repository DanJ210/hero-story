using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public class StorySessionConfiguration : IEntityTypeConfiguration<StorySession>
{
    public void Configure(EntityTypeBuilder<StorySession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Genre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.HeroArchetype).HasMaxLength(100).IsRequired();
        builder.Property(x => x.HeroName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasMany(x => x.Scenes).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
