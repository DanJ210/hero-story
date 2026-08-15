using System.Net;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace HeroStory.UnitTests.Services;

public class OpenAiClientTests
{
    [Fact]
    public async Task IsFlaggedAsync_UsesCurrentDefaultModerationModel()
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

        var flagged = await client.IsFlaggedAsync("safe input", CancellationToken.None);

        Assert.False(flagged);
        Assert.Contains("omni-moderation-latest", requestBody);
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