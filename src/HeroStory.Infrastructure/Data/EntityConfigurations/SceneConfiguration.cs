using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChoiceText).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.NarrativeText).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasOne(x => x.GenerationJob).WithOne(x => x.Scene).HasForeignKey<GenerationJob>(x => x.SceneId);
        builder.HasIndex(x => new { x.SessionId, x.SequenceNumber }).IsUnique();
    }
}
