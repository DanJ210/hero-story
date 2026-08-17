using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.ChoiceText).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.NarrativeText).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.SceneSummary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ActiveConflict).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.StoryStateSchemaVersion).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.StoryStateJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.SuggestedActionsJson).HasMaxLength(2000).HasDefaultValue("[]").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired().IsConcurrencyToken();
        builder.HasMany(x => x.GenerationJobs).WithOne(x => x.Scene).HasForeignKey(x => x.SceneId);
        builder.HasOne<Scene>().WithMany().HasForeignKey(x => x.ParentSceneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scene>().WithMany().HasForeignKey(x => x.RevisedFromSceneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SessionId, x.SequenceNumber }).IsUnique().HasFilter("[IsActive] = 1");
    }
}
