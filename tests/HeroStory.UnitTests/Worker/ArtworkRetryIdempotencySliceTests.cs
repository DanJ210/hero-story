using System.Text.Json;
using Azure.Storage.Queues.Models;
using HeroStory.Api.Services;
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
/// Vertical slice over queue message -> worker -> real placeholder strategy -> blob upload -> persisted scene artwork.
/// Proves bounded automatic retry and that terminal jobs never regenerate on redelivery.
/// </summary>
public class ArtworkRetryIdempotencySliceTests
{
    private const int MaxDequeueCount = 3;

    [Theory]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Poisoned)]
    public async Task Redelivery_OfTerminalJob_SkipsGenerationAndAcknowledgesMessage(JobStatus terminalStatus)
    {
        await using var harness = await SliceHarness.CreateAsync(terminalStatus);
        harness.Scene.ImageUrl = "https://blob.test/scenes/existing.png";
        await harness.DbContext.SaveChangesAsync();

        await harness.ProcessAsync(dequeueCount: 2);

        Assert.Empty(harness.Blob.UploadedBlobNames);
        Assert.Equal(terminalStatus, harness.Job.Status);
        Assert.Equal(0, harness.Job.AttemptCount);
        Assert.Equal("https://blob.test/scenes/existing.png", harness.Scene.ImageUrl);
        harness.VerifyDeleted(Times.Once());
        harness.VerifyPoisoned(Times.Never());
    }

    [Fact]
    public async Task TransientFailure_BelowMaxDequeueCount_MarksJobFailedAndLeavesMessageForRetry()
    {
        await using var harness = await SliceHarness.CreateAsync();
        harness.Blob.FailUploadsUntilAttempt = int.MaxValue;

        await harness.ProcessAsync(dequeueCount: 1);

        Assert.Equal(JobStatus.Failed, harness.Job.Status);
        Assert.Equal(1, harness.Job.AttemptCount);
        Assert.Null(harness.Scene.ImageUrl);
        harness.VerifyDeleted(Times.Never());
        harness.VerifyPoisoned(Times.Never());
    }

    [Fact]
    public async Task RepeatedTransientFailures_PoisonJobOnceRedeliveryBudgetIsExhausted()
    {
        await using var harness = await SliceHarness.CreateAsync();
        harness.Blob.FailUploadsUntilAttempt = int.MaxValue;

        await harness.ProcessAsync(dequeueCount: 1);
        Assert.Equal(JobStatus.Failed, harness.Job.Status);

        await harness.ProcessAsync(dequeueCount: 2);
        Assert.Equal(JobStatus.Failed, harness.Job.Status);
        Assert.Equal(2, harness.Job.AttemptCount);
        harness.VerifyPoisoned(Times.Never());

        await harness.ProcessAsync(dequeueCount: MaxDequeueCount);

        Assert.Equal(JobStatus.Poisoned, harness.Job.Status);
        Assert.Equal(3, harness.Job.AttemptCount);
        Assert.Equal(3, harness.Blob.UploadAttempts);
        Assert.Null(harness.Scene.ImageUrl);
        harness.VerifyPoisoned(Times.Once());
        harness.VerifyDeleted(Times.Once());
    }

    [Fact]
    public async Task SuccessfulRedelivery_AfterFailedAttempt_CompletesJobAndAttachesArtwork()
    {
        await using var harness = await SliceHarness.CreateAsync();
        harness.Blob.FailUploadsUntilAttempt = 1;

        await harness.ProcessAsync(dequeueCount: 1);
        Assert.Equal(JobStatus.Failed, harness.Job.Status);
        Assert.Null(harness.Scene.ImageUrl);

        await harness.ProcessAsync(dequeueCount: 2);

        Assert.Equal(JobStatus.Completed, harness.Job.Status);
        Assert.Equal(2, harness.Job.AttemptCount);
        Assert.NotNull(harness.Job.CompletedAt);
        Assert.Equal($"https://blob.test/scenes/{harness.Scene.Id}/placeholder.png", harness.Scene.ImageUrl);
        Assert.Single(harness.Blob.UploadedBlobNames);
        harness.VerifyDeleted(Times.Once());
        harness.VerifyPoisoned(Times.Never());
    }

    [Fact]
    public async Task RedeliveryAfterCompletion_DoesNotRegenerateArtwork()
    {
        await using var harness = await SliceHarness.CreateAsync();

        await harness.ProcessAsync(dequeueCount: 1);
        Assert.Equal(JobStatus.Completed, harness.Job.Status);
        var completedAt = harness.Job.CompletedAt;
        var imageUrl = harness.Scene.ImageUrl;

        await harness.ProcessAsync(dequeueCount: 2);

        Assert.Equal(JobStatus.Completed, harness.Job.Status);
        Assert.Equal(1, harness.Job.AttemptCount);
        Assert.Equal(completedAt, harness.Job.CompletedAt);
        Assert.Equal(imageUrl, harness.Scene.ImageUrl);
        Assert.Single(harness.Blob.UploadedBlobNames);
        harness.VerifyDeleted(Times.Exactly(2));
        harness.VerifyPoisoned(Times.Never());
    }

    /// <summary>Records artwork side effects so tests can prove uploads happen only when generation is expected.</summary>
    private sealed class RecordingBlobStorageService : IBlobStorageService
    {
        public List<string> UploadedBlobNames { get; } = [];

        public int UploadAttempts { get; private set; }

        /// <summary>Upload attempts at or below this number throw, simulating a transient storage outage.</summary>
        public int FailUploadsUntilAttempt { get; set; }

        public Task<string> UploadPlaceholderAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
        {
            UploadAttempts++;
            if (UploadAttempts <= FailUploadsUntilAttempt)
            {
                throw new IOException("Transient blob storage failure.");
            }

            UploadedBlobNames.Add(blobName);
            return Task.FromResult(GenerateImageAccessUrl(blobName));
        }

        public string GenerateImageAccessUrl(string blobName) => $"https://blob.test/{blobName}";

        public string GenerateImageUrl(string blobName) => GenerateImageAccessUrl(blobName);

        public Task<string> UploadImageAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
            => UploadPlaceholderAsync(blobName, content, contentType, cancellationToken);

        public Task DeleteImageAsync(string blobName, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SliceHarness : IAsyncDisposable
    {
        private readonly ImageGenerationWorker _worker;
        private readonly string _messageText;

        private SliceHarness(AppDbContext dbContext, GenerationJob job, Scene scene)
        {
            DbContext = dbContext;
            Job = job;
            Scene = scene;
            Queue = new Mock<AzureQueueClient>();
            Blob = new RecordingBlobStorageService();

            var services = new ServiceCollection();
            // Singleton so the shared context survives the per-message scope the worker disposes.
            services.AddSingleton(DbContext);
            services.AddSingleton<IBlobStorageService>(Blob);
            services.AddScoped<IImageGeneratorStrategy, PlaceholderImageStrategy>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["IMAGE_STRATEGY"] = "placeholder" })
                .Build();
            _worker = new ImageGenerationWorker(
                services.BuildServiceProvider(),
                Queue.Object,
                Options.Create(new WorkerOptions { MaxDequeueCount = MaxDequeueCount, PollIntervalSeconds = 1 }),
                NullLogger<ImageGenerationWorker>.Instance,
                configuration);
            _messageText = JsonSerializer.Serialize(new { jobId = job.Id, sceneId = scene.Id, sessionId = scene.SessionId });
        }

        public AppDbContext DbContext { get; }

        public GenerationJob Job { get; }

        public Scene Scene { get; }

        public Mock<AzureQueueClient> Queue { get; }

        public RecordingBlobStorageService Blob { get; }

        public static async Task<SliceHarness> CreateAsync(JobStatus status = JobStatus.Queued)
        {
            var dbContext = LikenessWorkerFixture.CreateDbContext(Guid.NewGuid().ToString());
            var session = LikenessWorkerFixture.CreateSession(Guid.NewGuid(), likenessEnabled: false);
            var scene = LikenessWorkerFixture.CreateScene(session.Id);
            var job = LikenessWorkerFixture.CreateLikenessJob(scene, portrait: null, DateTime.UtcNow, status);
            dbContext.StorySessions.Add(session);
            dbContext.Scenes.Add(scene);
            dbContext.GenerationJobs.Add(job);
            await dbContext.SaveChangesAsync();
            return new SliceHarness(dbContext, job, scene);
        }

        public Task ProcessAsync(long dequeueCount)
        {
            var message = QueuesModelFactory.QueueMessage(
                messageId: Guid.NewGuid().ToString(),
                popReceipt: "pop-receipt",
                body: new BinaryData(_messageText),
                dequeueCount: dequeueCount,
                nextVisibleOn: DateTimeOffset.UtcNow.AddMinutes(1),
                insertedOn: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffset.UtcNow.AddHours(1));
            return _worker.ProcessMessageAsync(message, CancellationToken.None);
        }

        public void VerifyDeleted(Times times)
            => Queue.Verify(queue => queue.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), times);

        public void VerifyPoisoned(Times times)
            => Queue.Verify(queue => queue.MoveToPoisonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), times);

        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
    }
}
