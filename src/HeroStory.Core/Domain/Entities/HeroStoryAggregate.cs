using HeroStory.Core.Domain.Enums;

namespace HeroStory.Core.Domain.Entities;

public sealed class HeroStoryAggregate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string HeroName { get; set; } = string.Empty;
    public string Setting { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public StoryStatus Status { get; set; } = StoryStatus.Draft;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CoverImageUrl { get; set; }
    public string? FailureReason { get; set; }
    public List<StoryScene> Scenes { get; } = [];

    public void Touch() => UpdatedUtc = DateTimeOffset.UtcNow;
}
