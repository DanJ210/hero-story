namespace HeroStory.Core.Entities;

public sealed class PortraitAuditEvent
{
    public Guid Id { get; init; }
    public Guid SubjectUserId { get; init; }
    public string ActorType { get; init; } = string.Empty;
    public Guid? ActorUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid? PortraitId { get; init; }
    public Guid? ConsentRecordId { get; init; }
    public Guid? RelatedPortraitId { get; init; }
    public Guid? SessionId { get; init; }
    public Guid? SceneId { get; init; }
    public Guid? GenerationJobId { get; init; }
    public string? DetailCode { get; init; }
    public DateTime OccurredAt { get; init; }
}
