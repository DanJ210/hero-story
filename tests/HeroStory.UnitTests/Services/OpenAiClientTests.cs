using System.Net;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace HeroStory.UnitTests.Services;

public class OpenAiClientTests
{
    [Fact]
    public async Task GetFlaggedCategoriesAsync_UsesCurrentDefaultModerationModel()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"flagged\":false}]}")
            };
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var client = new OpenAiClient(new HttpClient(handler), configuration);

        var flagged = await client.GetFlaggedCategoriesAsync("safe input", CancellationToken.None);

        Assert.Empty(flagged);
        Assert.Contains("omni-moderation-latest", requestBody);
    }

    [Fact]
    public async Task GetFlaggedCategoriesAsync_ReturnsOnlyTrueCategories()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\":[{\"flagged\":true,\"categories\":{\"violence\":true,\"sexual\":false}}]}")
        }));
        var client = new OpenAiClient(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection().Build());

        var flagged = await client.GetFlaggedCategoriesAsync("a battle scene", CancellationToken.None);

        Assert.Equal(["violence"], flagged);
    }

    [Fact]
    public async Task GetFlaggedCategoriesAsync_TreatsFlaggedWithoutCategoriesAsBlocking()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\":[{\"flagged\":true}]}")
        }));
        var client = new OpenAiClient(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection().Build());

        var flagged = await client.GetFlaggedCategoriesAsync("unknown", CancellationToken.None);

        Assert.Equal([OpenAiClient.UnspecifiedModerationCategory], flagged);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}