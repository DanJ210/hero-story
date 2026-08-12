using HeroStory.Infrastructure.Clients;

namespace HeroStory.Api.Services;

public class OpenAiTextService : IOpenAiTextService
{
    private readonly OpenAiClient _client;

    public OpenAiTextService(OpenAiClient client)
    {
        _client = client;
    }

    public Task<string> GenerateNarrativeAsync(string prompt, CancellationToken cancellationToken)
        => _client.CreateChatCompletionAsync(prompt, cancellationToken);
}
