using HeroStory.Api.DTOs.Scene;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HeroStory.UnitTests.Services;

public class SceneServiceTests
{
    [Fact]
    public async Task CreateSceneAsync_CreatesQueuedScene()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Story",
            Genre = "Fantasy",
            HeroArchetype = "Mage",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.StorySessions.Add(session);
        await dbContext.SaveChangesAsync();

        var moderation = new Mock<IModerationService>();
        moderation.Setup(x => x.ModerateInputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModerationStatus.Approved, null as string));
        moderation.Setup(x => x.ModerateOutputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModerationStatus.Approved, null as string, "Narrative"));
        var text = new Mock<IOpenAiTextService>();
        text.Setup(x => x.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GeneratedStoryTurn(
            "Narrative",
            "Ari enters the northern pass.",
            "Northern pass",
            "Find a safe route through the storm",
            "{\"facts\":[\"The northern pass is blocked\"]}",
            ["Climb the ridge", "Search for shelter"],
            StoryBeat.Major,
            false));
        var queue = new Mock<AzureQueueClient>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        queue.Setup(x => x.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = new SceneService(dbContext, moderation.Object, text.Object, queue.Object);
        var result = await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Go north"), CancellationToken.None);

        Assert.Equal(1, result.SequenceNumber);
        Assert.Equal(ModerationStatus.Approved, result.ModerationStatus);
        Assert.Equal("Ari enters the northern pass.", result.SceneSummary);
        Assert.Equal("Northern pass", result.Location);
        Assert.Equal(1, result.StoryStateSchemaVersion);
        Assert.Equal(2, result.SuggestedActions.Count);
        Assert.Equal(StoryBeat.Major, result.StoryBeat);
        Assert.False(result.IsEpisodeComplete);

        var storedScene = await dbContext.Scenes.SingleAsync();
        Assert.Equal(result.SceneSummary, storedScene.SceneSummary);
        Assert.Contains("northern pass", storedScene.StoryStateJson);
    }
}
