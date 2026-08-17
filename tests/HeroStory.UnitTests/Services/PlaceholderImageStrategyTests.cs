using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using HeroStory.Worker;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Services;

public class PlaceholderImageStrategyTests
{
    [Fact]
    public async Task GenerateAsync_StoresSignedImageAccessUrl()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hero@example.com", Email = "hero@example.com", DisplayName = "Hero", CreatedAt = DateTime.UtcNow };
        var session = new StorySession { Id = Guid.NewGuid(), UserId = user.Id, Title = "Story", Genre = "Superhero", HeroArchetype = "Guardian", HeroName = "Ari", User = user, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var scene = new Scene { Id = Guid.NewGuid(), SessionId = session.Id, SequenceNumber = 1, ChoiceText = "Begin", NarrativeText = "Narrative", SceneSummary = "Summary", Location = "City", ActiveConflict = "Threat", Session = session, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var job = new GenerationJob { Id = Guid.NewGuid(), SceneId = scene.Id, SessionId = session.Id, Prompt = "Prompt", Scene = scene, Status = JobStatus.Processing, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        scene.GenerationJobs.Add(job);
        dbContext.AddRange(user, session, scene, job);
        await dbContext.SaveChangesAsync();
        var blobStorage = new Mock<IBlobStorageService>();
        blobStorage.Setup(storage => storage.UploadPlaceholderAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>())).ReturnsAsync("http://127.0.0.1/raw-private-url");
        blobStorage.Setup(storage => storage.GenerateImageAccessUrl(It.IsAny<string>())).Returns("http://127.0.0.1/signed-url?sig=test");
        var strategy = new PlaceholderImageStrategy(blobStorage.Object, dbContext);

        await strategy.GenerateAsync(job, CancellationToken.None);

        Assert.Equal("http://127.0.0.1/signed-url?sig=test", scene.ImageUrl);
        Assert.Equal(JobStatus.Completed, job.Status);
        blobStorage.Verify(storage => storage.GenerateImageAccessUrl($"scenes/{scene.Id}/placeholder.png"), Times.Once);
    }
}