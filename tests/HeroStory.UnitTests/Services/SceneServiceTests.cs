using System.Linq;
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
        moderation.Setup(x => x.ModerateOutputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModerationStatus.Approved, null as string, CreateNarrative()));
        var text = new Mock<IOpenAiTextService>();
        text.Setup(x => x.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GeneratedStoryTurn(
            CreateNarrative(),
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
        Assert.Equal(StoryBeat.Opening, result.StoryBeat);
        Assert.Equal(ArtworkStatus.Queued, result.ArtworkStatus);
        Assert.False(result.IsEpisodeComplete);

        var storedScene = await dbContext.Scenes.SingleAsync();
        Assert.Equal(result.SceneSummary, storedScene.SceneSummary);
        Assert.Contains("northern pass", storedScene.StoryStateJson);
        Assert.Single(dbContext.GenerationJobs);
        queue.Verify(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSceneAsync_DoesNotQueueArtworkForLaterStandardTurn()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Standard turn");
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "OPENING_STATE"));
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = CreateQueue();
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(StoryBeat.Standard).Object, queue.Object);

        var result = await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Continue patrol"), CancellationToken.None);

        Assert.Equal(StoryBeat.Standard, result.StoryBeat);
        Assert.Equal(ArtworkStatus.NotRequested, result.ArtworkStatus);
        Assert.Empty(dbContext.GenerationJobs);
        queue.Verify(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StoryBeat.Major)]
    [InlineData(StoryBeat.Climax)]
    [InlineData(StoryBeat.Conclusion)]
    public async Task CreateSceneAsync_QueuesArtworkForQualifyingLaterBeat(StoryBeat storyBeat)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Illustrated turn");
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "OPENING_STATE"));
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = CreateQueue();
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(storyBeat).Object, queue.Object);

        var result = await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Face the threat"), CancellationToken.None);

        Assert.Equal(storyBeat, result.StoryBeat);
        Assert.Equal(ArtworkStatus.Queued, result.ArtworkStatus);
        Assert.Single(dbContext.GenerationJobs);
        queue.Verify(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSceneAsync_MapsFailedArtworkStatus()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Failed artwork");
        var scene = CreatePreviousScene(session.Id, 1, "FAILED_ARTWORK");
        session.Scenes.Add(scene);
        dbContext.Add(session);
        dbContext.GenerationJobs.Add(new GenerationJob
        {
            Id = Guid.NewGuid(),
            SceneId = scene.Id,
            SessionId = session.Id,
            Prompt = "Image prompt",
            Status = JobStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new SceneService(dbContext, new Mock<IModerationService>().Object, new Mock<IOpenAiTextService>().Object, CreateQueue().Object);
        var result = await service.GetSceneAsync(session.UserId, session.Id, scene.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ArtworkStatus.Failed, result.ArtworkStatus);
    }

    [Fact]
    public async Task CreateSceneAsync_IncludesLatestOwnedStoryStateInPrompt()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var userId = Guid.NewGuid();
        var session = CreateSession(userId, "Owned story");
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "EARLIER_STATE"));
        session.Scenes.Add(CreatePreviousScene(session.Id, 2, "LATEST_OWNED_STATE"));
        var unrelatedSession = CreateSession(Guid.NewGuid(), "Unrelated story");
        unrelatedSession.Scenes.Add(CreatePreviousScene(unrelatedSession.Id, 1, "OTHER_USER_STATE"));
        dbContext.AddRange(session, unrelatedSession);
        await dbContext.SaveChangesAsync();

        var moderation = CreateApprovedModeration();
        var capturedPrompt = string.Empty;
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync(CreateGeneratedTurn());
        var queue = CreateQueue();

        var service = new SceneService(dbContext, moderation.Object, text.Object, queue.Object);
        await service.CreateSceneAsync(userId, session.Id, new CreateSceneRequest("Protect the city"), CancellationToken.None);

        Assert.Contains("LATEST_OWNED_STATE", capturedPrompt);
        Assert.Contains("Latest accepted scene summary", capturedPrompt);
        Assert.Contains("Previous narrative passage", capturedPrompt);
        Assert.DoesNotContain("EARLIER_STATE", capturedPrompt);
        Assert.DoesNotContain("OTHER_USER_STATE", capturedPrompt);
    }

    [Fact]
    public async Task CreateSceneAsync_UsesExplicitEmptyContextForFirstTurn()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "First turn");
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var moderation = CreateApprovedModeration();
        var capturedPrompt = string.Empty;
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync(CreateGeneratedTurn());
        var queue = CreateQueue();

        var service = new SceneService(dbContext, moderation.Object, text.Object, queue.Object);
        await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Begin the patrol"), CancellationToken.None);

        Assert.Contains("This is the opening turn; no prior story state exists", capturedPrompt);
    }

    [Fact]
    public async Task CreateSceneAsync_RejectsUnsupportedPersistedStateVersion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Future state");
        var previousScene = CreatePreviousScene(session.Id, 1, "FUTURE_STATE");
        previousScene.StoryStateSchemaVersion = 2;
        session.Scenes.Add(previousScene);
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var service = new SceneService(dbContext, CreateApprovedModeration().Object, new Mock<IOpenAiTextService>().Object, CreateQueue().Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Continue"), CancellationToken.None));

        Assert.Contains("schema version 2", exception.Message);
    }

    private static StorySession CreateSession(Guid userId, string title)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Scene CreatePreviousScene(Guid sessionId, int sequenceNumber, string stateMarker)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = sequenceNumber,
            ChoiceText = "Previous action",
            NarrativeText = $"Previous narrative passage containing {stateMarker}.",
            SceneSummary = $"Previous summary containing {stateMarker}.",
            Location = "Previous location",
            ActiveConflict = "Previous conflict",
            StoryStateJson = System.Text.Json.JsonSerializer.Serialize(new { facts = new[] { stateMarker } }),
            SuggestedActionsJson = "[\"One\",\"Two\"]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Mock<IModerationService> CreateApprovedModeration()
    {
        var moderation = new Mock<IModerationService>();
        moderation.Setup(service => service.ModerateInputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModerationStatus.Approved, null as string));
        moderation.Setup(service => service.ModerateOutputAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModerationStatus.Approved, null as string, CreateNarrative()));
        return moderation;
    }

    private static Mock<AzureQueueClient> CreateQueue()
    {
        var queue = new Mock<AzureQueueClient>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        queue.Setup(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return queue;
    }

    private static Mock<IOpenAiTextService> CreateTextService(StoryBeat storyBeat)
    {
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGeneratedTurn(storyBeat));
        return text;
    }

    private static GeneratedStoryTurn CreateGeneratedTurn(StoryBeat storyBeat = StoryBeat.Standard)
        => new(
            CreateNarrative(),
            "Ari protects the city.",
            "City center",
            "Stop the attack",
            "{\"facts\":[\"The city is under attack\"]}",
            ["Protect civilians", "Confront the attacker"],
            storyBeat,
            false);

    private static string CreateNarrative(int wordCount = 250)
        => string.Join(' ', Enumerable.Repeat("hero", wordCount));
}
