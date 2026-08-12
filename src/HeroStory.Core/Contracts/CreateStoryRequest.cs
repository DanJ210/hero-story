namespace HeroStory.Core.Contracts;

public sealed class CreateStoryRequest
{
    public string HeroName { get; init; } = string.Empty;
    public string Setting { get; init; } = string.Empty;
    public string Tone { get; init; } = "hopeful";
    public string Prompt { get; init; } = string.Empty;
}
