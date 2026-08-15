namespace HeroStory.IntegrationTests.Scenes;

public class SceneEndpointTests
{
    [Fact]
    public void SceneDto_SerializesStoryBeatAsCamelCaseString()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        var dto = new HeroStory.Api.DTOs.Scene.SceneDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Act",
            "Narrative",
            "Summary",
            "Location",
            "Conflict",
            1,
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}"),
            ["One", "Two"],
            HeroStory.Core.Enums.StoryBeat.Major,
            false,
            null,
            null,
            HeroStory.Core.Enums.ModerationStatus.Approved,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);

        var json = System.Text.Json.JsonSerializer.Serialize(dto, options);

        Assert.Contains("\"storyBeat\":\"major\"", json);
    }
}
