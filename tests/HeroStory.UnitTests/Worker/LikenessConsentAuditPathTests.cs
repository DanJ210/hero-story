using System.Linq;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using HeroStory.Worker;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Worker;

public class LikenessConsentAuditPathTests
{
    [Fact]
    public async Task PortraitGrantToProviderUsePersistsConsentProvenanceAndAuditEvents()
    {
        var configuration = LikenessWorkerFixture.CreateConfiguration();
        await using var dbContext = LikenessWorkerFixture.CreateDbContext(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var portraitBlobs = new Mock<AzureBlobService>(configuration) { CallBase = false };
        portraitBlobs
            .Setup(service => service.UploadAsync(
                LikenessWorkerFixture.PortraitsContainer,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                "image/jpeg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("private-blob-reference");
        var portraitService = new UserPortraitService(dbContext, portraitBlobs.Object, configuration);

        await using var uploadedContent = new MemoryStream([1, 2, 3]);
        await portraitService.UploadAsync(
            userId, uploadedContent, "image/jpeg", uploadedContent.Length, true, PortraitConsentPolicy.PolicyVersion, CancellationToken.None);

        var consent = await dbContext.PortraitConsentRecords.SingleAsync();
        var session = LikenessWorkerFixture.CreateSession(userId, likenessEnabled: false);
        var scene = LikenessWorkerFixture.CreateScene(session.Id);
        dbContext.StorySessions.Add(session);
        dbContext.Scenes.Add(scene);
        await dbContext.SaveChangesAsync();

        var queue = new Mock<AzureQueueClient>();
        queue.Setup(client => client.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sceneService = new SceneService(
            dbContext,
            new Mock<IModerationService>().Object,
            new Mock<IOpenAiTextService>().Object,
            queue.Object,
            portraitService);

        await sceneService.RequestArtworkAsync(userId, session.Id, scene.Id, true, CancellationToken.None);

        var job = await dbContext.GenerationJobs.SingleAsync();
        Assert.Equal(consent.PortraitId, job.PortraitId);
        Assert.Equal(consent.Id, job.PortraitConsentRecordId);

        var handler = new RecordingOpenAiHandler();
        var imageStorage = new Mock<IBlobStorageService>();
        imageStorage
            .Setup(storage => storage.UploadPlaceholderAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://images.test/generated");
        imageStorage.Setup(storage => storage.GenerateImageAccessUrl(It.IsAny<string>()))
            .Returns("https://images.test/generated");
        var workerBlobService = LikenessWorkerFixture.CreatePortraitBlobService(configuration);
        var strategy = new DallE3Strategy(
            imageStorage.Object,
            dbContext,
            LikenessWorkerFixture.CreateOpenAiClient(handler, configuration),
            workerBlobService.Object,
            configuration);

        await strategy.GenerateAsync(job, CancellationToken.None);

        Assert.True(handler.ImageCallMade);
        Assert.Equal(HeroStory.Core.Enums.JobStatus.Completed, job.Status);
        Assert.Equal("https://images.test/generated", scene.ImageUrl);
        var auditEvents = await dbContext.PortraitAuditEvents.OrderBy(audit => audit.EventType).ToListAsync();
        var requested = Assert.Single(auditEvents.Where(audit => audit.EventType == "likeness_use_requested"));
        var started = Assert.Single(auditEvents.Where(audit => audit.EventType == "likeness_provider_use_started"));
        Assert.Equal(userId, requested.ActorUserId);
        Assert.Equal(userId, requested.SubjectUserId);
        Assert.Equal(session.Id, requested.SessionId);
        Assert.Equal(scene.Id, requested.SceneId);
        Assert.Equal(job.Id, requested.GenerationJobId);
        Assert.Equal(consent.Id, requested.ConsentRecordId);
        Assert.Equal("worker", started.ActorType);
        Assert.Equal(consent.Id, started.ConsentRecordId);
        Assert.Equal(job.Id, started.GenerationJobId);
        Assert.Null(started.DetailCode);
    }
}
