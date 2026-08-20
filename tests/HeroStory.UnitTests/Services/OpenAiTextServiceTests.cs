using System.Net;
using System.Net.Http.Json;
using System.Linq;
using HeroStory.Api.Services;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HeroStory.UnitTests.Services;

public class OpenAiTextServiceTests
{
    [Fact]
    public async Task GenerateTurnAsync_RetriesParserFailureAndReturnsValidTurn()
    {
        var responses = new Queue<string>([
            "{\"narrative\":\"too short\"}",
            CreateValidResponse()
        ]);
        var client = CreateClient(responses);
        var configuration = CreateConfiguration(maxRetries: 1);
        var service = new OpenAiTextService(client, configuration, NullLogger<OpenAiTextService>.Instance);

        var result = await service.GenerateTurnAsync("story prompt", CancellationToken.None);

        Assert.Equal(StoryBeat.Standard, result.StoryBeat);
        Assert.Equal(2, clientCalls);
    }

    [Fact]
    public async Task GenerateTurnAsync_StopsAfterConfiguredParserRetries()
    {
        var responses = new Queue<string>(["{\"narrative\":\"too short\"}", "{\"narrative\":\"still too short\"}"]);
        var client = CreateClient(responses);
        var service = new OpenAiTextService(client, CreateConfiguration(maxRetries: 1), NullLogger<OpenAiTextService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateTurnAsync("story prompt", CancellationToken.None));
        Assert.Equal(2, clientCalls);
    }

    private int clientCalls;

    private OpenAiClient CreateClient(Queue<string> responses)
    {
        clientCalls = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            clientCalls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    choices = new[] { new { message = new { content = responses.Dequeue() } } }
                })
            });
        });
        return new OpenAiClient(new HttpClient(handler), CreateConfiguration());
    }

    private static IConfiguration CreateConfiguration(int maxRetries = 0)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OPENAI_TEXT_MAX_RETRIES"] = maxRetries.ToString(),
            ["OPENAI_TEXT_RETRY_DELAY_MS"] = "0"
        }).Build();

    private static string CreateValidResponse()
        => $$"""
        {
          "narrative": "{{string.Join(' ', Enumerable.Repeat("hero", 250))}}",
          "sceneSummary": "The hero faces the threat.",
          "location": "Central plaza",
          "activeConflict": "Protect the civilians",
          "storyState": { "facts": ["The plaza is under threat"] },
          "suggestedActions": ["Protect civilians", "Confront the threat"],
          "storyBeat": "standard",
          "isEpisodeComplete": false
        }
        """;

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
