using HeroStory.Infrastructure.Clients;

namespace HeroStory.Api.Services;

public class OpenAiTextService : IOpenAiTextService
{
    private readonly OpenAiClient _openAiClient;

    public OpenAiTextService(OpenAiClient openAiClient)
    {
        _openAiClient = openAiClient;
    }

    public Task<string> GenerateNarrativeAsync(string prompt, CancellationToken cancellationToken)
        => _openAiClient.CreateChatCompletionAsync(prompt, cancellationToken);
}
