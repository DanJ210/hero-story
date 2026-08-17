using global::HeroStory.Api.Services;
using global::HeroStory.Infrastructure.Data;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Worker;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Worker;

public class PlaceholderImageStrategyTests
{
    [Fact]
    public async Task GenerateAsync_DoesNotAttachArtworkToSupersededScene()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Story",
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var scene = new Scene
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SequenceNumber = 1,
            IsActive = false,
            ChoiceText = "Original move",
            NarrativeText = "Original passage",
            SceneSummary = "Original summary",
            Location = "City",
            ActiveConflict = "Threat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var job = new GenerationJob
        {
            Id = Guid.NewGuid(),
            SceneId = scene.Id,
            SessionId = session.Id,
            Prompt = "Artwork prompt",
            Status = JobStatus.Processing,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.AddRange(session, scene, job);
        await dbContext.SaveChangesAsync();

        var blobStorage = new Mock<IBlobStorageService>();
        var strategy = new PlaceholderImageStrategy(blobStorage.Object, dbContext);

        await strategy.GenerateAsync(job, CancellationToken.None);

        Assert.Null(scene.ImageUrl);
        Assert.Equal(JobStatus.Completed, job.Status);
        blobStorage.Verify(storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}