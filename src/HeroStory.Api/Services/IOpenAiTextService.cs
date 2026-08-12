namespace HeroStory.Api.Services;

public interface IOpenAiTextService
{
    Task<string> GenerateNarrativeAsync(string prompt, CancellationToken cancellationToken);
}
