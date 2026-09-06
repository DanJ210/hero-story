using System.Text.Json;
using Azure.Storage.Queues.Models;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using HeroStory.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HeroStory.UnitTests.Worker;

/// <summary>
/// Covers the queue-message half of the worker: redelivery, terminal-job idempotency,
/// and the dequeue-count mapping that decides Failed versus Poisoned.
/// </summary>
public class ImageGenerationWorkerQueueTests
{
    private const int MaxDequeueCount = 3;

    [Fact]
    public async Task ProcessMessageAsync_MapsPolicyFailureToFailedBelowMaxDequeueCount()
    {
        var harness = await QueueHarness.CreateAsync();
        var strategy = new StubStrategy(_ => throw new ArtworkPolicyException(
            ArtworkErrorCode.PortraitReferenceExpired, "The portrait likeness reference expired."));
        var worker = harness.CreateWorker(strategy);

        await worker.ProcessMessageAsync(harness.CreateMessage(dequeueCount: 1), CancellationToken.None);

        Assert.Equal(JobStatus.Failed, harness.Job.Status);
        Assert.Equal($"{ArtworkErrorCode.PortraitReferenceExpired}: The portrait likeness reference expired.", harness.Job.ErrorDetail);
        Assert.Equal(1, harness.Job.AttemptCount);
        // The message stays on the queue so bounded redelivery can retry it.
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_PoisonsPolicyFailureAtMaxDequeueCount()
    {
        var harness = await QueueHarness.CreateAsync();
        var strategy = new StubStrategy(_ => throw new ArtworkPolicyException(
            ArtworkErrorCode.PortraitConsentMissing, "Likeness provenance is missing consent timestamp."));
        var worker = harness.CreateWorker(strategy);

        await worker.ProcessMessageAsync(harness.CreateMessage(dequeueCount: MaxDequeueCount), CancellationToken.None);

        Assert.Equal(JobStatus.Poisoned, harness.Job.Status);
        Assert.StartsWith($"{ArtworkErrorCode.PortraitConsentMissing}:", harness.Job.ErrorDetail);
        harness.Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_PoisonsWithoutGeneratingWhenDequeueCountExceedsLimit()
    {
        var harness = await QueueHarness.CreateAsync();
        var strategy = new StubStrategy(_ => throw new InvalidOperationException("Generation must not be attempted."));
        var worker = harness.CreateWorker(strategy);

        await worker.ProcessMessageAsync(harness.CreateMessage(dequeueCount: MaxDequeueCount + 1), CancellationToken.None);

        Assert.False(strategy.WasInvoked);
        Assert.Equal(JobStatus.Queued, harness.Job.Status);
        harness.Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Poisoned)]
    public async Task ProcessMessageAsync_SkipsTerminalJobsOnRedelivery(JobStatus terminalStatus)
    {
        var harness = await QueueHarness.CreateAsync(terminalStatus);
        var strategy = new StubStrategy(_ => throw new InvalidOperationException("Terminal jobs must not regenerate."));
        var worker = harness.CreateWorker(strategy);

        await worker.ProcessMessageAsync(harness.CreateMessage(dequeueCount: 1), CancellationToken.None);

        Assert.False(strategy.WasInvoked);
        Assert.Equal(terminalStatus, harness.Job.Status);
        Assert.Equal(0, harness.Job.AttemptCount);
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_DeletesMessageAfterSuccessfulGeneration()
    {
        var harness = await QueueHarness.CreateAsync();
        var strategy = new StubStrategy(job => job.Status = JobStatus.Completed);
        var worker = harness.CreateWorker(strategy);

        await worker.ProcessMessageAsync(harness.CreateMessage(dequeueCount: 1), CancellationToken.None);

        Assert.True(strategy.WasInvoked);
        Assert.Equal(JobStatus.Completed, harness.Job.Status);
        Assert.Equal(1, harness.Job.AttemptCount);
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_DropsStaleMessageForMissingJob()
    {
        var harness = await QueueHarness.CreateAsync();
        var strategy = new StubStrategy(_ => throw new InvalidOperationException("Missing jobs must not generate."));
        var worker = harness.CreateWorker(strategy);

        var staleMessage = harness.CreateMessage(dequeueCount: 1, jobId: Guid.NewGuid());
        await worker.ProcessMessageAsync(staleMessage, CancellationToken.None);

        Assert.False(strategy.WasInvoked);
        harness.Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class StubStrategy : IImageGeneratorStrategy
    {
        private readonly Action<GenerationJob> _onGenerate;

        public StubStrategy(Action<GenerationJob> onGenerate) => _onGenerate = onGenerate;

        public bool WasInvoked { get; private set; }

        public string Name => "stub";

        public Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            _onGenerate(job);
            return Task.CompletedTask;
        }
    }

    private sealed class QueueHarness
    {
        private QueueHarness(AppDbContext dbContext, GenerationJob job, Scene scene)
        {
            DbContext = dbContext;
            Job = job;
            Scene = scene;
            Queue = new Mock<AzureQueueClient>();
        }

        public AppDbContext DbContext { get; }

        public GenerationJob Job { get; }

        public Scene Scene { get; }

        public Mock<AzureQueueClient> Queue { get; }

        public static async Task<QueueHarness> CreateAsync(JobStatus status = JobStatus.Queued)
        {
            var dbContext = LikenessWorkerFixture.CreateDbContext(Guid.NewGuid().ToString());
            var session = LikenessWorkerFixture.CreateSession(Guid.NewGuid());
            var scene = LikenessWorkerFixture.CreateScene(session.Id);
            var portrait = LikenessWorkerFixture.CreatePortrait(session.UserId, DateTime.UtcNow.AddMinutes(-5));
            var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait, DateTime.UtcNow, status);
            dbContext.StorySessions.Add(session);
            dbContext.Scenes.Add(scene);
            dbContext.UserPortraits.Add(portrait);
            dbContext.GenerationJobs.Add(job);
            await dbContext.SaveChangesAsync();
            return new QueueHarness(dbContext, job, scene);
        }

        public ImageGenerationWorker CreateWorker(IImageGeneratorStrategy strategy)
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => DbContext);
            services.AddSingleton(strategy);
            var options = Options.Create(new WorkerOptions { MaxDequeueCount = MaxDequeueCount, PollIntervalSeconds = 1 });
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["IMAGE_STRATEGY"] = "stub" })
                .Build();
            return new ImageGenerationWorker(
                services.BuildServiceProvider(),
                Queue.Object,
                options,
                NullLogger<ImageGenerationWorker>.Instance,
                configuration);
        }

        public QueueMessage CreateMessage(long dequeueCount, Guid? jobId = null)
        {
            var payload = JsonSerializer.Serialize(new
            {
                jobId = jobId ?? Job.Id,
                sceneId = Scene.Id,
                sessionId = Scene.SessionId
            });
            return QueuesModelFactory.QueueMessage(
                messageId: Guid.NewGuid().ToString(),
                popReceipt: "pop-receipt",
                body: new BinaryData(payload),
                dequeueCount: dequeueCount,
                nextVisibleOn: DateTimeOffset.UtcNow.AddMinutes(1),
                insertedOn: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
