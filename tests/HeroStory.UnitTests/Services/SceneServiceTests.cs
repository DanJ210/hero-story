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

    [Fact]
    public async Task CreateSceneAsync_OptedInAutomaticLikenessStoresPortraitProvenance()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Automatic likeness");
        session.LikenessEnabled = true;
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "OPENING_STATE"));
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var portraitId = Guid.NewGuid();
        var consentGrantedAt = DateTime.UtcNow;
        var portraits = new Mock<IUserPortraitService>();
        portraits.Setup(service => service.GetActiveReferenceAsync(session.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPortraitReference(portraitId, consentGrantedAt));
        var queue = CreateQueue();
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(StoryBeat.Climax).Object, queue.Object, portraits.Object);

        await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Face the threat"), CancellationToken.None);

        var job = await dbContext.GenerationJobs.SingleAsync();
        Assert.Equal(portraitId, job.PortraitId);
        Assert.Equal(consentGrantedAt, job.PortraitConsentGrantedAt);
        portraits.Verify(item => item.GetActiveReferenceAsync(session.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSceneAsync_OptedInAutomaticLikenessRejectsWithoutActivePortrait()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Automatic likeness without portrait");
        session.LikenessEnabled = true;
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "OPENING_STATE"));
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var portraits = new Mock<IUserPortraitService>();
        portraits.Setup(service => service.GetActiveReferenceAsync(session.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as UserPortraitReference);
        var queue = CreateQueue();
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(StoryBeat.Climax).Object, queue.Object, portraits.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Face the threat"), CancellationToken.None));

        Assert.Contains("active consented portrait", exception.Message);
        Assert.Empty(dbContext.GenerationJobs);
        queue.Verify(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestArtworkAsync_QueuesManualRequestAndAllowsAnotherAfterCompletion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Manual artwork");
        var scene = CreatePreviousScene(session.Id, 1, "MANUAL_ARTWORK");
        session.Scenes.Add(scene);
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = CreateQueue();
        var service = new SceneService(dbContext, new Mock<IModerationService>().Object, new Mock<IOpenAiTextService>().Object, queue.Object);

        var firstRequest = await service.RequestArtworkAsync(session.UserId, session.Id, scene.Id, false, CancellationToken.None);
        var pendingException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestArtworkAsync(session.UserId, session.Id, scene.Id, false, CancellationToken.None));
        var firstJob = await dbContext.GenerationJobs.SingleAsync();
        firstJob.Status = JobStatus.Completed;
        firstJob.CompletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var secondRequest = await service.RequestArtworkAsync(session.UserId, session.Id, scene.Id, false, CancellationToken.None);

        Assert.Equal(ArtworkStatus.Queued, firstRequest.ArtworkStatus);
        Assert.Contains("already being generated", pendingException.Message);
        Assert.Equal(ArtworkStatus.Queued, secondRequest.ArtworkStatus);
        Assert.Equal(2, await dbContext.GenerationJobs.CountAsync());
        queue.Verify(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RequestArtworkAsync_WithPortraitStoresOpaquePortraitProvenance()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Likeness artwork");
        var scene = CreatePreviousScene(session.Id, 1, "PORTRAIT_SCENE");
        session.Scenes.Add(scene);
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var portraitId = Guid.NewGuid();
        var portraits = new Mock<IUserPortraitService>();
        portraits.Setup(service => service.GetActiveReferenceAsync(session.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPortraitReference(portraitId, DateTime.UtcNow));
        var service = new SceneService(dbContext, new Mock<IModerationService>().Object, new Mock<IOpenAiTextService>().Object, CreateQueue().Object, portraits.Object);

        await service.RequestArtworkAsync(session.UserId, session.Id, scene.Id, true, CancellationToken.None);

        var job = await dbContext.GenerationJobs.SingleAsync();
        Assert.Equal(portraitId, job.PortraitId);
        Assert.NotNull(job.PortraitConsentGrantedAt);
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
        Assert.Contains("EARLIER_STATE", capturedPrompt);
        Assert.DoesNotContain("OTHER_USER_STATE", capturedPrompt);
    }

    [Fact]
    public async Task CreateSceneAsync_BoundsOlderContinuityContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Bounded continuity");
        for (var sequenceNumber = 1; sequenceNumber <= 30; sequenceNumber++)
        {
            var scene = CreatePreviousScene(session.Id, sequenceNumber, new string('S', 5000) + sequenceNumber);
            session.Scenes.Add(scene);
        }
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var capturedPrompt = string.Empty;
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync(CreateGeneratedTurn());
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, text.Object, CreateQueue().Object);

        await service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Continue"), CancellationToken.None);

        Assert.True(capturedPrompt.Length < 20000);
        Assert.Contains("Latest accepted scene summary", capturedPrompt);
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

    [Fact]
    public async Task ReviseLatestSceneAsync_SupersedesTargetAndReturnsReplacementOnActivePath()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Revision story");
        var parent = CreatePreviousScene(session.Id, 1, "PARENT_STATE");
        var target = CreatePreviousScene(session.Id, 2, "SUPERSEDED_STATE");
        target.ParentSceneId = parent.Id;
        session.Scenes.Add(parent);
        session.Scenes.Add(target);
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var capturedPrompt = string.Empty;
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync(CreateGeneratedTurn());
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, text.Object, CreateQueue().Object);

        var replacement = await service.ReviseLatestSceneAsync(
            session.UserId,
            session.Id,
            target.Id,
            new ReviseSceneRequest("Try a different route"),
            CancellationToken.None);

        var storedTarget = await dbContext.Scenes.SingleAsync(scene => scene.Id == target.Id);
        var storedReplacement = await dbContext.Scenes.SingleAsync(scene => scene.Id == replacement.Id);
        var activeScenes = await service.GetScenesAsync(session.UserId, session.Id, CancellationToken.None);

        Assert.False(storedTarget.IsActive);
        Assert.True(storedReplacement.IsActive);
        Assert.Equal(target.SequenceNumber, storedReplacement.SequenceNumber);
        Assert.Equal(parent.Id, storedReplacement.ParentSceneId);
        Assert.Equal(target.Id, storedReplacement.RevisedFromSceneId);
        Assert.DoesNotContain(activeScenes, scene => scene.Id == target.Id);
        Assert.Equal(new[] { parent.Id, storedReplacement.Id }, activeScenes.Select(scene => scene.Id));
        Assert.Contains("PARENT_STATE", capturedPrompt);
        Assert.DoesNotContain("SUPERSEDED_STATE", capturedPrompt);
    }

    [Fact]
    public async Task ReviseLatestSceneAsync_RejectsNonLatestActiveScene()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Revision order");
        var first = CreatePreviousScene(session.Id, 1, "FIRST_STATE");
        var latest = CreatePreviousScene(session.Id, 2, "LATEST_STATE");
        latest.ParentSceneId = first.Id;
        session.Scenes.Add(first);
        session.Scenes.Add(latest);
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(StoryBeat.Standard).Object, CreateQueue().Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviseLatestSceneAsync(
            session.UserId,
            session.Id,
            first.Id,
            new ReviseSceneRequest("Change the beginning"),
            CancellationToken.None));

        Assert.Contains("latest active", exception.Message);
    }

    [Fact]
    public async Task ReviseLatestSceneAsync_AllowsOnlyOneConcurrentReplacement()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options;
        var userId = Guid.NewGuid();
        var session = CreateSession(userId, "Concurrent revision");
        var target = CreatePreviousScene(session.Id, 1, "TARGET_STATE");
        session.Scenes.Add(target);

        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Add(session);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var generationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var generatedTurn = new TaskCompletionSource<GeneratedStoryTurn>(TaskCreationOptions.RunContinuationsAsynchronously);
        var generationCalls = 0;
        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((_, _) =>
            {
                if (Interlocked.Increment(ref generationCalls) == 2)
                {
                    generationStarted.TrySetResult();
                }

                return generatedTurn.Task;
            });
        var moderation = CreateApprovedModeration();
        var firstService = new SceneService(firstContext, moderation.Object, text.Object, CreateQueue().Object);
        var secondService = new SceneService(secondContext, moderation.Object, text.Object, CreateQueue().Object);

        var firstRevision = firstService.ReviseLatestSceneAsync(userId, session.Id, target.Id, new ReviseSceneRequest("First replacement"), CancellationToken.None);
        var secondRevision = secondService.ReviseLatestSceneAsync(userId, session.Id, target.Id, new ReviseSceneRequest("Second replacement"), CancellationToken.None);
        await generationStarted.Task;
        generatedTurn.TrySetResult(CreateGeneratedTurn());

        await Assert.ThrowsAnyAsync<DbUpdateConcurrencyException>(() => Task.WhenAll(firstRevision, secondRevision));

        await using var verificationContext = new AppDbContext(options);
        var scenes = await verificationContext.Scenes.Where(scene => scene.SessionId == session.Id).ToListAsync();
        Assert.Single(scenes.Where(scene => scene.IsActive));
        Assert.Single(scenes.Where(scene => !scene.IsActive));
    }

    [Fact]
    public async Task ConcludeEpisodeAsync_CompletesSessionWhenProviderConfirmsCompletion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Episode conclusion");
        session.Scenes.Add(CreatePreviousScene(session.Id, 1, "UNRESOLVED_THREAT"));
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var text = new Mock<IOpenAiTextService>();
        text.Setup(service => service.GenerateTurnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGeneratedTurn(StoryBeat.Conclusion, true));
        var service = new SceneService(dbContext, CreateApprovedModeration().Object, text.Object, CreateQueue().Object);

        var conclusion = await service.ConcludeEpisodeAsync(session.UserId, session.Id, CancellationToken.None);

        Assert.True(conclusion.IsEpisodeComplete);
        Assert.Equal(StoryBeat.Conclusion, conclusion.StoryBeat);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.Equal(2, conclusion.SequenceNumber);
    }

    [Fact]
    public async Task CreateSceneAsync_RejectsPausedEpisode()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = CreateSession(Guid.NewGuid(), "Paused episode");
        session.Status = SessionStatus.Paused;
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();

        var service = new SceneService(dbContext, CreateApprovedModeration().Object, CreateTextService(StoryBeat.Standard).Object, CreateQueue().Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSceneAsync(session.UserId, session.Id, new CreateSceneRequest("Continue anyway"), CancellationToken.None));

        Assert.Contains("not active", exception.Message);
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

    private static GeneratedStoryTurn CreateGeneratedTurn(StoryBeat storyBeat = StoryBeat.Standard, bool isEpisodeComplete = false)
        => new(
            CreateNarrative(),
            "Ari protects the city.",
            "City center",
            "Stop the attack",
            "{\"facts\":[\"The city is under attack\"]}",
            ["Protect civilians", "Confront the attacker"],
            storyBeat,
            isEpisodeComplete);

    private static string CreateNarrative(int wordCount = 250)
        => string.Join(' ', Enumerable.Repeat("hero", wordCount));
}
