namespace HeroStory.Core.Entities;

public sealed class PortraitConsentRecord
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid PortraitId { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public string PolicyVersion { get; init; } = string.Empty;
    public string ProviderScope { get; init; } = string.Empty;
    public DateTime GrantedAt { get; init; }
}
