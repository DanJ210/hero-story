using System.Net;
using System.Text.Json;
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

    [Fact]
    public async Task GenerateImageWithReferenceAsync_SendsDataUrlAndReturnsDecodedImageBytes()
    {
        string? requestBody = null;
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"b64_json\":\"AQIDBA==\"}]}")
            };
        });
        var client = new OpenAiClient(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection().Build());
        await using var referenceImage = new MemoryStream([0x10, 0x20, 0x30, 0x40]);

        var result = await client.GenerateImageWithReferenceAsync("portrait prompt", referenceImage, "image/jpeg", CancellationToken.None);

        Assert.Equal(expectedBytes, result);
        Assert.NotNull(requestBody);
        using var json = JsonDocument.Parse(requestBody!);
        var imageUrl = json.RootElement.GetProperty("images")[0].GetProperty("image_url").GetString();
        Assert.NotNull(imageUrl);
        Assert.StartsWith("data:image/jpeg;base64,", imageUrl, StringComparison.Ordinal);

        var encoded = imageUrl!["data:image/jpeg;base64,".Length..];
        var decodedReferenceBytes = Convert.FromBase64String(encoded);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0x40 }, decodedReferenceBytes);
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