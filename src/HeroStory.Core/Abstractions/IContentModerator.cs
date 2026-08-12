namespace HeroStory.Core.Abstractions;

public interface IContentModerator
{
    bool IsAllowed(string text, out string? reason);
}
