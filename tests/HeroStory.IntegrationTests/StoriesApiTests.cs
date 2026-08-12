using System.Net;
using System.Net.Http.Json;
using HeroStory.Api.Contracts;
using HeroStory.Core.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HeroStory.IntegrationTests;

public sealed class StoriesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StoriesApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostStory_ReturnsCreatedStory()
    {
        var response = await _client.PostAsJsonAsync("/api/stories", new CreateStoryApiRequest
        {
            HeroName = "Ari",
            Setting = "Sky Harbor",
            Tone = "playful",
            Prompt = "the wind has stolen every kite in town"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var story = await response.Content.ReadFromJsonAsync<StoryDto>();
        Assert.NotNull(story);
        Assert.Equal("Ari", story!.HeroName);
        Assert.Equal(3, story.Scenes.Count);
    }
}
