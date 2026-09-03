using System.Linq;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HeroStory.UnitTests.Services;

public class UserPortraitServiceTests
{
    [Fact]
    public async Task UploadAsync_ReplacesActivePortraitWithoutClearingSessionLikenessOptIn()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var userId = Guid.NewGuid();
        var previousPortrait = CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-10));
        dbContext.UserPortraits.Add(previousPortrait);
        dbContext.StorySessions.Add(CreateSession(userId, likenessEnabled: true));
        await dbContext.SaveChangesAsync();
        var blobService = CreateBlobService();
        var service = new UserPortraitService(dbContext, blobService.Object, CreateConfiguration());

        await using var content = new MemoryStream([1, 2, 3]);
        var replacement = await service.UploadAsync(userId, content, "image/jpeg", content.Length, true, CancellationToken.None);

        Assert.NotEqual(previousPortrait.Id, replacement.Id);
        Assert.NotNull(previousPortrait.DisabledAt);
        Assert.True(await dbContext.StorySessions.AnyAsync(session => session.UserId == userId && session.LikenessEnabled));
        var activeReference = await service.GetActiveReferenceAsync(userId, CancellationToken.None);
        Assert.Equal(replacement.Id, activeReference?.Id);
        blobService.Verify(client => client.UploadAsync("test-portraits", It.IsAny<string>(), It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableAsync_DisablesActivePortraitsAndTurnsOffSessionLikenessOptIn()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var userId = Guid.NewGuid();
        dbContext.UserPortraits.Add(CreatePortrait(userId, DateTime.UtcNow));
        dbContext.StorySessions.Add(CreateSession(userId, likenessEnabled: true));
        await dbContext.SaveChangesAsync();
        var service = new UserPortraitService(dbContext, CreateBlobService().Object, CreateConfiguration());

        var disabled = await service.DisableAsync(userId, CancellationToken.None);

        Assert.True(disabled);
        Assert.Null(await service.GetActiveReferenceAsync(userId, CancellationToken.None));
        Assert.False(await dbContext.StorySessions.AnyAsync(session => session.UserId == userId && session.LikenessEnabled));
        Assert.All(dbContext.UserPortraits.Where(portrait => portrait.UserId == userId), portrait => Assert.NotNull(portrait.DisabledAt));
    }

    [Fact]
    public async Task DeleteAsync_PurgesAllPortraitVersionsAndSettlesOutstandingLikenessJobs()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var userId = Guid.NewGuid();
var supersededPortrait = CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-20));
supersededPortrait.BlobName = $"users/{userId}/portraits/superseded";
supersededPortrait.DisabledAt = DateTime.UtcNow.AddMinutes(-10);
var latestPortrait = CreatePortrait(userId, DateTime.UtcNow);
latestPortrait.BlobName = $"users/{userId}/portraits/latest";
dbContext.UserPortraits.AddRange(supersededPortrait, latestPortrait);
        dbContext.StorySessions.Add(session);
        dbContext.GenerationJobs.AddRange(
            CreateLikenessJob(session.Id, latestPortrait.Id, JobStatus.Queued),
            CreateLikenessJob(session.Id, latestPortrait.Id, JobStatus.Processing),
            CreateLikenessJob(session.Id, supersededPortrait.Id, JobStatus.Failed),
            CreateLikenessJob(session.Id, supersededPortrait.Id, JobStatus.Completed));
        await dbContext.SaveChangesAsync();
        var blobService = CreateBlobService();
        var service = new UserPortraitService(dbContext, blobService.Object, CreateConfiguration());

        var deleted = await service.DeleteAsync(userId, CancellationToken.None);

        Assert.True(deleted);
        Assert.All(dbContext.UserPortraits.Where(portrait => portrait.UserId == userId), portrait =>
        {
            Assert.NotNull(portrait.DisabledAt);
            Assert.NotNull(portrait.DeletedAt);
        });
        Assert.False(await dbContext.StorySessions.AnyAsync(session => session.UserId == userId && session.LikenessEnabled));
        var settledJobs = await dbContext.GenerationJobs
            .Where(job => job.PortraitId == latestPortrait.Id || job.PortraitId == supersededPortrait.Id)
            .OrderBy(job => job.Status)
            .ToListAsync();
        Assert.Equal(3, settledJobs.Count(job => job.Status == JobStatus.Poisoned));
        Assert.Single(settledJobs.Where(job => job.Status == JobStatus.Completed));
        Assert.All(settledJobs.Where(job => job.Status == JobStatus.Poisoned), job =>
        {
            Assert.NotNull(job.CompletedAt);
            Assert.Contains("PortraitDeleted", job.ErrorDetail);
        });
        blobService.Verify(client => client.DeleteAsync("test-portraits", supersededPortrait.BlobName, It.IsAny<CancellationToken>()), Times.Once);
        blobService.Verify(client => client.DeleteAsync("test-portraits", latestPortrait.BlobName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeAsync_ReturnsCountsForDeletedPortraitsBlobsAndSettledJobs()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var dbContext = new AppDbContext(options);
        var userId = Guid.NewGuid();
        var firstPortrait = CreatePortrait(userId, DateTime.UtcNow.AddMinutes(-5));
        var secondPortrait = CreatePortrait(userId, DateTime.UtcNow);
        secondPortrait.BlobName = $"users/{userId}/portraits/second";
        dbContext.UserPortraits.AddRange(firstPortrait, secondPortrait);
        var session = CreateSession(userId, likenessEnabled: true);
        dbContext.StorySessions.Add(session);
        dbContext.GenerationJobs.Add(CreateLikenessJob(session.Id, secondPortrait.Id, JobStatus.Queued));
        await dbContext.SaveChangesAsync();

        var service = new UserPortraitService(dbContext, CreateBlobService().Object, CreateConfiguration());
        var result = await service.PurgeAsync(userId, CancellationToken.None);

        Assert.Equal(2, result.PortraitsDeleted);
        Assert.Equal(2, result.BlobsRemoved);
        Assert.Equal(1, result.JobsSettled);
    }

    private static UserPortrait CreatePortrait(Guid userId, DateTime createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlobName = $"users/{userId}/portraits/test",
            ContentType = "image/jpeg",
            ContentLength = 1024,
            ConsentGrantedAt = createdAt,
            CreatedAt = createdAt
        };

    private static StorySession CreateSession(Guid userId, bool likenessEnabled)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Story",
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            LikenessEnabled = likenessEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static GenerationJob CreateLikenessJob(Guid sessionId, Guid portraitId, JobStatus status)
        => new()
        {
            Id = Guid.NewGuid(),
            SceneId = Guid.NewGuid(),
            SessionId = sessionId,
            PortraitId = portraitId,
            PortraitConsentGrantedAt = DateTime.UtcNow.AddMinutes(-1),
            Prompt = "Portrait job",
            Status = status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

    private static Mock<AzureBlobService> CreateBlobService()
    {
        var blobService = new Mock<AzureBlobService>(CreateConfiguration());
        blobService.Setup(client => client.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example/portrait");
        blobService.Setup(client => client.GenerateSasUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://storage.example/portrait?sas=1");
        blobService.Setup(client => client.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return blobService;
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AZURE_BLOB_CONNECTION_STRING"] = "UseDevelopmentStorage=true",
            ["AZURE_BLOB_PORTRAITS_CONTAINER"] = "test-portraits"
        }).Build();
}