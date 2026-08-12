namespace HeroStory.Core.Contracts;

public sealed class StorySceneDto
{
    public int Sequence { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Narrative { get; init; } = string.Empty;
    public string ImagePrompt { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}
