namespace HeroStory.Core.Contracts;

public sealed class StoryDto
{
    public Guid Id { get; init; }
    public string HeroName { get; init; } = string.Empty;
    public string Setting { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
    public IReadOnlyCollection<StorySceneDto> Scenes { get; init; } = [];
}
