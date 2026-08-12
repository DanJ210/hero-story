using HeroStory.Core.Contracts;
using HeroStory.Core.Services;
using HeroStory.Infrastructure.Options;
using HeroStory.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HeroStory.UnitTests;

public sealed class StoryServiceTests
{
    private static StoryService CreateService()
    {
        var repo = new InMemoryStoryRepository();
        var queue = new InMemoryImageJobQueue();
        var moderator = new FileKeywordModerator(
            Options.Create(new ModerationOptions { KeywordListPath = "missing-keywords.txt" }),
            NullLogger<FileKeywordModerator>.Instance);
        var generator = new PlaceholderImageGenerator();
        return new StoryService(repo, queue, moderator, generator);
    }

    [Fact]
    public async Task CreateAsync_BuildsThreeScenes()
    {
        var service = CreateService();
        var story = await service.CreateAsync(new CreateStoryRequest
        {
            HeroName = "Nova",
            Setting = "Moonbase Echo",
            Tone = "uplifting",
            Prompt = "a meteor shower threatens the solar garden"
        }, CancellationToken.None);

        Assert.Equal("Draft", story.Status);
        Assert.Equal(3, story.Scenes.Count);
    }

    [Fact]
    public async Task ProcessImagesAsync_MarksStoryReady()
    {
        var service = CreateService();
        var created = await service.CreateAsync(new CreateStoryRequest
        {
            HeroName = "Nova",
            Setting = "Moonbase Echo",
            Tone = "uplifting",
            Prompt = "a meteor shower threatens the solar garden"
        }, CancellationToken.None);

        var processed = await service.ProcessImagesAsync(created.Id, CancellationToken.None);
        var loaded = await service.GetAsync(created.Id, CancellationToken.None);

        Assert.True(processed);
        Assert.NotNull(loaded);
        Assert.Equal("Ready", loaded!.Status);
        Assert.All(loaded.Scenes, scene => Assert.False(string.IsNullOrWhiteSpace(scene.ImageUrl)));
    }
}
