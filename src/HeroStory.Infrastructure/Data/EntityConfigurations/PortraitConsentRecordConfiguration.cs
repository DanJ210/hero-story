using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public sealed class PortraitConsentRecordConfiguration : IEntityTypeConfiguration<PortraitConsentRecord>
{
    public void Configure(EntityTypeBuilder<PortraitConsentRecord> builder)
    {
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(record => record.PolicyVersion).HasMaxLength(100).IsRequired();
        builder.Property(record => record.ProviderScope).HasMaxLength(100).IsRequired();
        builder.Property(record => record.GrantedAt).IsRequired();
        builder.HasIndex(record => new { record.UserId, record.PortraitId, record.GrantedAt });
        builder.HasOne<UserPortrait>()
            .WithMany(portrait => portrait.ConsentRecords)
            .HasForeignKey(record => record.PortraitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
