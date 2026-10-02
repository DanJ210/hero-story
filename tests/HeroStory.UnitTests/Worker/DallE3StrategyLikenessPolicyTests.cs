using System.Linq;
using HeroStory.Api.Services;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using HeroStory.Worker;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Worker;

/// <summary>
/// Exercises the real worker generation path so likeness policy violations are proven to fail closed
/// end to end, not just inside <see cref="PortraitLikenessPolicy"/>.
/// </summary>
public class DallE3StrategyLikenessPolicyTests
{
    [Fact]
    public async Task GenerateAsync_FailsClosedWhenJobProvenanceHasNoConsentRecord()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        job.PortraitConsentRecordId = null;
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(job, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitConsentMissing, exception.Code);
        AssertFailedClosed(job, scene, handler, imageStorage, ArtworkErrorCode.PortraitConsentMissing);
        Assert.Contains(dbContext.PortraitAuditEvents, audit => audit.EventType == "likeness_use_rejected");
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenConsentWasRevoked()
    {
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(Guid.NewGuid().ToString());
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        var consent = portrait.ConsentRecords.Single();
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        dbContext.PortraitAuditEvents.Add(new HeroStory.Core.Entities.PortraitAuditEvent
        {
            Id = Guid.NewGuid(),
            SubjectUserId = userId,
            ActorType = "user",
            ActorUserId = userId,
            EventType = "consent_revoked",
            PortraitId = portrait.Id,
            ConsentRecordId = consent.Id,
            OccurredAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(job, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitConsentMissing, exception.Code);
        AssertFailedClosed(job, scene, handler, imageStorage, ArtworkErrorCode.PortraitConsentMissing);
        Assert.Contains(dbContext.PortraitAuditEvents, audit => audit.EventType == "likeness_use_rejected");
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenConsentedPortraitIsNoLongerActive()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        portrait.DisabledAt = DateTime.UtcNow;
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(job, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitUnavailable, exception.Code);
        AssertFailedClosed(job, scene, handler, imageStorage, ArtworkErrorCode.PortraitUnavailable);
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenProviderReferenceExpiredBeforeGeneration()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var consentGrantedAt = DateTime.UtcNow.AddMinutes(-180);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, consentGrantedAt);
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, consentGrantedAt);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage, referenceMaxAgeMinutes: 120);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(job, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitReferenceExpired, exception.Code);
        AssertFailedClosed(job, scene, handler, imageStorage, ArtworkErrorCode.PortraitReferenceExpired);
    }

    [Fact]
    public async Task GenerateAsync_DiscardsLikenessArtworkWhenSceneIsSupersededMidFlight()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        // Supersede the scene through a separate context while the provider call is in flight.
        var handler = new RecordingOpenAiHandler(async () =>
        {
            await using var concurrent = LikenessWorkerFixture.CreateDbContext(databaseName);
            var superseded = await concurrent.Scenes.SingleAsync(candidate => candidate.Id == scene.Id);
            superseded.IsActive = false;
            await concurrent.SaveChangesAsync();
        });
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage);

        await strategy.GenerateAsync(job, CancellationToken.None);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.True(handler.ImageCallMade);
        imageStorage.Verify(
            storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        await using var verification = LikenessWorkerFixture.CreateDbContext(databaseName);
        var persistedScene = await verification.Scenes.SingleAsync(candidate => candidate.Id == scene.Id);
        Assert.False(persistedScene.IsActive);
        Assert.Null(persistedScene.ImageUrl);
        Assert.Null(persistedScene.ImageUrlExpiresAt);
        Assert.Contains(await verification.PortraitAuditEvents.ToListAsync(), audit => audit.EventType == "likeness_provider_use_started");
    }

    [Fact]
    public async Task GenerateAsync_DiscardsProviderResultWhenConsentIsRevokedDuringCall()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        var configuration = LikenessWorkerFixture.CreateConfiguration();
        var handler = new RecordingOpenAiHandler(async () =>
        {
            await using var revocationContext = LikenessWorkerFixture.CreateDbContext(databaseName);
            var blobService = new Mock<HeroStory.Infrastructure.Storage.AzureBlobService>(configuration) { CallBase = false };
            var portraitService = new UserPortraitService(revocationContext, blobService.Object, configuration);
            await portraitService.PurgeAsync(userId, CancellationToken.None);
        });
        var imageStorage = new Mock<IBlobStorageService>();
        var strategy = CreateStrategy(dbContext, handler, imageStorage);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(job, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitUnavailable, exception.Code);
        Assert.True(handler.ImageCallMade);
        Assert.Equal(JobStatus.Poisoned, job.Status);
        Assert.Null(scene.ImageUrl);
        imageStorage.Verify(
            storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        await using var verification = LikenessWorkerFixture.CreateDbContext(databaseName);
        Assert.Contains(await verification.PortraitAuditEvents.ToListAsync(), audit => audit.EventType == "consent_revoked");
        Assert.Contains(await verification.PortraitAuditEvents.ToListAsync(), audit => audit.EventType == "likeness_use_rejected");
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenPortraitIsDeletedWhileJobIsInFlight()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.Add(job);
        await dbContext.SaveChangesAsync();

        var configuration = LikenessWorkerFixture.CreateConfiguration();
        await using (var deletionContext = LikenessWorkerFixture.CreateDbContext(databaseName))
        {
            var portraitBlobs = new Mock<HeroStory.Infrastructure.Storage.AzureBlobService>(configuration) { CallBase = false };
            var portraitService = new UserPortraitService(deletionContext, portraitBlobs.Object, configuration);
            var purge = await portraitService.PurgeAsync(userId, CancellationToken.None);

            Assert.Equal(1, purge.PortraitsDeleted);
            Assert.Equal(1, purge.JobsSettled);
            portraitBlobs.Verify(
                blobs => blobs.DeleteAsync(LikenessWorkerFixture.PortraitsContainer, portrait.BlobName, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        await using (var settled = LikenessWorkerFixture.CreateDbContext(databaseName))
        {
            var settledJob = await settled.GenerationJobs.SingleAsync(candidate => candidate.Id == job.Id);
            Assert.Equal(JobStatus.Poisoned, settledJob.Status);
            Assert.Contains("PortraitDeleted", settledJob.ErrorDetail);
            Assert.False(await settled.StorySessions.AnyAsync(candidate => candidate.UserId == userId && candidate.LikenessEnabled));
        }

        // A redelivered message for the settled job must still refuse to reach the provider.
        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        await using var workerContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var redeliveredJob = await workerContext.GenerationJobs.SingleAsync(candidate => candidate.Id == job.Id);
        var strategy = CreateStrategy(workerContext, handler, imageStorage);

        var exception = await Assert.ThrowsAsync<ArtworkPolicyException>(
            () => strategy.GenerateAsync(redeliveredJob, CancellationToken.None));

        Assert.Equal(ArtworkErrorCode.PortraitUnavailable, exception.Code);
        Assert.False(handler.ImageCallMade);
        imageStorage.Verify(
            storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        var persistedScene = await workerContext.Scenes.SingleAsync(candidate => candidate.Id == scene.Id);
        Assert.Null(persistedScene.ImageUrl);
    }

    [Fact]
    public async Task DeleteAccountAsync_SettlesInFlightLikenessJobsAndRetainsCompletedArtwork()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(databaseName);
        var session = LikenessWorkerFixture.CreateSession(userId);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        scene.ImageUrl = "https://blob.test/scenes/generated.png";
        var portrait = LikenessWorkerFixture.CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var inFlight = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow, JobStatus.Processing);
        var alreadyDelivered = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow.AddMinutes(-30), JobStatus.Completed);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        dbContext.UserPortraits.Add(portrait);
        dbContext.GenerationJobs.AddRange(inFlight, alreadyDelivered);
        await dbContext.SaveChangesAsync();

        var configuration = LikenessWorkerFixture.CreateConfiguration();
        var portraitBlobs = new Mock<HeroStory.Infrastructure.Storage.AzureBlobService>(configuration) { CallBase = false };
        var portraitService = new UserPortraitService(dbContext, portraitBlobs.Object, configuration);

        var purge = await portraitService.PurgeAsync(userId, CancellationToken.None);

        Assert.Equal(1, purge.JobsSettled);
        Assert.Equal(JobStatus.Poisoned, inFlight.Status);
        Assert.Contains("PortraitDeleted", inFlight.ErrorDetail);
        // Retention policy: artwork already generated from the deleted source stays with the story.
        Assert.Equal(JobStatus.Completed, alreadyDelivered.Status);
        Assert.Equal("https://blob.test/scenes/generated.png", scene.ImageUrl);
    }

    private static DallE3Strategy CreateStrategy(
        AppDbContext dbContext,
        RecordingOpenAiHandler handler,
        Mock<IBlobStorageService> imageStorage,
        int referenceMaxAgeMinutes = 120)
    {
        var configuration = LikenessWorkerFixture.CreateConfiguration(referenceMaxAgeMinutes);
        var openAiClient = LikenessWorkerFixture.CreateOpenAiClient(handler, configuration);
        var portraitBlobs = LikenessWorkerFixture.CreatePortraitBlobService(configuration);
        return new DallE3Strategy(imageStorage.Object, dbContext, openAiClient, portraitBlobs.Object, configuration);
    }

    private static void AssertFailedClosed(
        Core.Entities.GenerationJob job,
        Core.Entities.Scene scene,
        RecordingOpenAiHandler handler,
        Mock<IBlobStorageService> imageStorage,
        string expectedCode)
    {
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.StartsWith($"{expectedCode}:", job.ErrorDetail);
        Assert.False(handler.ImageCallMade);
        Assert.Null(scene.ImageUrl);
        Assert.Null(scene.ImageUrlExpiresAt);
        imageStorage.Verify(
            storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
