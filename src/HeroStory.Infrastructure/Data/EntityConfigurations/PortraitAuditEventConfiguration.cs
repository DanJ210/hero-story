using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeroStory.Infrastructure.Data.EntityConfigurations;

public sealed class PortraitAuditEventConfiguration : IEntityTypeConfiguration<PortraitAuditEvent>
{
    public void Configure(EntityTypeBuilder<PortraitAuditEvent> builder)
    {
        builder.HasKey(eventRecord => eventRecord.Id);
        builder.Property(eventRecord => eventRecord.ActorType).HasMaxLength(20).IsRequired();
        builder.Property(eventRecord => eventRecord.EventType).HasMaxLength(80).IsRequired();
        builder.Property(eventRecord => eventRecord.DetailCode).HasMaxLength(80);
        builder.Property(eventRecord => eventRecord.OccurredAt).IsRequired();
        builder.HasIndex(eventRecord => new { eventRecord.SubjectUserId, eventRecord.OccurredAt });
        builder.HasIndex(eventRecord => eventRecord.ConsentRecordId);
        builder.HasIndex(eventRecord => eventRecord.GenerationJobId);
    }
}
