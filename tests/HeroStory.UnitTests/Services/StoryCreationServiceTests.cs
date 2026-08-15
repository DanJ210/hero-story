using HeroStory.Api.DTOs.Scene;
using HeroStory.Api.DTOs.Session;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Services;

public class StoryCreationServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsSessionAndOpeningScene()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessionDto = CreateSessionDto(sessionId);
        var openingScene = CreateSceneDto(sessionId);
        var storyService = new Mock<IStoryService>();
        storyService.Setup(service => service.CreateSessionAsync(userId, It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionDto);
        var sceneService = new Mock<ISceneService>();
        sceneService.Setup(service => service.CreateOpeningSceneAsync(userId, sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openingScene);
        var service = new StoryCreationService(storyService.Object, sceneService.Object, dbContext);

        var result = await service.CreateAsync(userId, new CreateSessionRequest("Origin", "Superhero", "Guardian", "Ari"), CancellationToken.None);

        Assert.Equal(sessionDto, result.Session);
        Assert.Equal(openingScene, result.OpeningScene);
    }

    [Fact]
    public async Task CreateAsync_RemovesNewSessionWhenOpeningGenerationFails()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Origin",
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.StorySessions.Add(session);
        await dbContext.SaveChangesAsync();
        var storyService = new Mock<IStoryService>();
        storyService.Setup(service => service.CreateSessionAsync(userId, It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSessionDto(session.Id));
        var sceneService = new Mock<ISceneService>();
        sceneService.Setup(service => service.CreateOpeningSceneAsync(userId, session.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Generation failed."));
        var service = new StoryCreationService(storyService.Object, sceneService.Object, dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(userId, new CreateSessionRequest("Origin", "Superhero", "Guardian", "Ari"), CancellationToken.None));

        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static SessionDto CreateSessionDto(Guid sessionId)
        => new(sessionId, "Origin", "Superhero", "Guardian", "Ari", SessionStatus.Active, 0, DateTime.UtcNow, DateTime.UtcNow);

    private static SceneDto CreateSceneDto(Guid sessionId)
        => new(
            Guid.NewGuid(),
            sessionId,
            1,
            "The story begins.",
            "Narrative",
            "Summary",
            "City",
            "A threat emerges",
            1,
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}"),
            ["Investigate", "Protect civilians"],
            StoryBeat.Opening,
            false,
            ArtworkStatus.Queued,
            null,
            null,
            ModerationStatus.Approved,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
}