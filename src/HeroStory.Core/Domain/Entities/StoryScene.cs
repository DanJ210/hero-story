namespace HeroStory.Core.Domain.Entities;

public sealed class StoryScene
{
    public int Sequence { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Narrative { get; init; } = string.Empty;
    public string ImagePrompt { get; init; } = string.Empty;
    public string? ImageUrl { get; set; }
}
