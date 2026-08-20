using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public class UserPortraitConfiguration : IEntityTypeConfiguration<UserPortrait>
{
    public void Configure(EntityTypeBuilder<UserPortrait> builder)
    {
        builder.HasKey(portrait => portrait.Id);
        builder.Property(portrait => portrait.BlobName).HasMaxLength(300).IsRequired();
        builder.Property(portrait => portrait.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(portrait => portrait.ContentLength).IsRequired();
        builder.Property(portrait => portrait.ConsentGrantedAt).IsRequired();
        builder.Property(portrait => portrait.CreatedAt).IsRequired();
        builder.HasIndex(portrait => new { portrait.UserId, portrait.CreatedAt });
        builder.HasOne(portrait => portrait.User).WithMany(user => user.Portraits).HasForeignKey(portrait => portrait.UserId);
    }
}