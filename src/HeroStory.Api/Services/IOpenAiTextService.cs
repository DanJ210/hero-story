using HeroStory.Api.DTOs.Scene;

namespace HeroStory.Api.Services;

public interface IOpenAiTextService
{
    Task<GeneratedStoryTurn> GenerateTurnAsync(string prompt, CancellationToken cancellationToken);
}
