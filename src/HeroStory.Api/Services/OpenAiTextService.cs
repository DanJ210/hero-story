using HeroStory.Api.DTOs.Scene;
using HeroStory.Infrastructure.Clients;

namespace HeroStory.Api.Services;

public class OpenAiTextService : IOpenAiTextService
{
    private readonly OpenAiClient _openAiClient;

    public OpenAiTextService(OpenAiClient openAiClient)
    {
        _openAiClient = openAiClient;
    }

    public async Task<GeneratedStoryTurn> GenerateTurnAsync(string prompt, CancellationToken cancellationToken)
    {
        var response = await _openAiClient.CreateChatCompletionAsync(prompt, cancellationToken);
        return StoryTurnResponseParser.Parse(response);
    }
}
