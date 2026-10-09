using HeroStory.Api.DTOs.Session;
using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HeroStory.UnitTests.Services;

public class StorySessionInputValidationTests
{
    private static readonly string MaxTitle = new('t', StorySessionFieldLimits.TitleMaxLength);
    private static readonly string MaxGenre = new('g', StorySessionFieldLimits.GenreMaxLength);
    private static readonly string MaxArchetype = new('a', StorySessionFieldLimits.HeroArchetypeMaxLength);
    private static readonly string MaxHeroName = new('n', StorySessionFieldLimits.HeroNameMaxLength);

    [Fact]
    public void FieldLimits_UseExpandedGenreAndArchetypeAndKeepTitleAndNameLimits()
    {
        Assert.Equal(200, StorySessionFieldLimits.TitleMaxLength);
        Assert.Equal(500, StorySessionFieldLimits.GenreMaxLength);
        Assert.Equal(1_000, StorySessionFieldLimits.HeroArchetypeMaxLength);
        Assert.Equal(100, StorySessionFieldLimits.HeroNameMaxLength);
    }

    [Fact]
    public void FieldLimits_CombinedOpeningModerationInputFitsModerationLimit()
    {
        // The opening scene moderates Title, Genre, HeroArchetype and HeroName joined by three newlines.
        var combined = string.Join('\n', MaxTitle, MaxGenre, MaxArchetype, MaxHeroName);

        Assert.True(combined.Length <= ModerationService.MaximumInputCharacters);
    }

    [Fact]
    public async Task CreateSessionAsync_AcceptsExactBoundariesAndPersistsFullText()
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);
        var userId = Guid.NewGuid();

        var result = await service.CreateSessionAsync(userId, new CreateSessionRequest(MaxTitle, MaxGenre, MaxArchetype, MaxHeroName), CancellationToken.None);

        var stored = await dbContext.StorySessions.SingleAsync();
        Assert.Equal(MaxTitle, stored.Title);
        Assert.Equal(MaxGenre, stored.Genre);
        Assert.Equal(MaxArchetype, stored.HeroArchetype);
        Assert.Equal(MaxHeroName, stored.HeroName);
        Assert.Equal(MaxGenre, result.Genre);
        Assert.Equal(MaxArchetype, result.HeroArchetype);
    }

    [Theory]
    [InlineData("title", "Title must be 200 characters or fewer.")]
    [InlineData("genre", "Genre must be 500 characters or fewer.")]
    [InlineData("archetype", "Hero archetype must be 1000 characters or fewer.")]
    [InlineData("name", "Hero name must be 100 characters or fewer.")]
    public async Task CreateSessionAsync_RejectsOversizedFieldWithoutPersisting(string field, string expectedMessage)
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSessionAsync(Guid.NewGuid(), CreateRequestWithOversized(field), CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
    }

    [Theory]
    [InlineData("", "Title is required.")]
    [InlineData("   ", "Title is required.")]
    public async Task CreateSessionAsync_RejectsBlankRequiredFields(string blank, string expectedMessage)
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSessionAsync(Guid.NewGuid(), new CreateSessionRequest(blank, "Superhero", "Guardian", "Ari"), CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
    }

    [Fact]
    public async Task CreateSessionAsync_ReportsEveryInvalidFieldInOneMessage()
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSessionAsync(Guid.NewGuid(), new CreateSessionRequest(" ", MaxGenre + "g", "Guardian", " "), CancellationToken.None));

        Assert.Equal("Title is required. Genre must be 500 characters or fewer. Hero name is required.", exception.Message);
    }

    [Fact]
    public async Task PatchSessionAsync_AcceptsExactBoundariesAndLeavesNullFieldsUnchanged()
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);
        var session = await SeedSessionAsync(dbContext);

        var result = await service.PatchSessionAsync(session.UserId, session.Id, new PatchSessionRequest(null, MaxGenre, MaxArchetype, null, null), CancellationToken.None);

        Assert.NotNull(result);
        var stored = await dbContext.StorySessions.SingleAsync();
        Assert.Equal("Origin", stored.Title);
        Assert.Equal(MaxGenre, stored.Genre);
        Assert.Equal(MaxArchetype, stored.HeroArchetype);
        Assert.Equal("Ari", stored.HeroName);
    }

    [Theory]
    [InlineData("title", "Title must be 200 characters or fewer.")]
    [InlineData("genre", "Genre must be 500 characters or fewer.")]
    [InlineData("archetype", "Hero archetype must be 1000 characters or fewer.")]
    [InlineData("name", "Hero name must be 100 characters or fewer.")]
    public async Task PatchSessionAsync_RejectsOversizedFieldWithoutChangingSession(string field, string expectedMessage)
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);
        var session = await SeedSessionAsync(dbContext);
        var request = new PatchSessionRequest(
            field == "title" ? MaxTitle + "t" : null,
            field == "genre" ? MaxGenre + "g" : "Changed genre",
            field == "archetype" ? MaxArchetype + "a" : null,
            field == "name" ? MaxHeroName + "n" : null,
            null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PatchSessionAsync(session.UserId, session.Id, request, CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        dbContext.ChangeTracker.Clear();
        var stored = await dbContext.StorySessions.SingleAsync();
        Assert.Equal("Superhero", stored.Genre);
        Assert.Equal("Guardian", stored.HeroArchetype);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task PatchSessionAsync_RejectsBlankValues(string blank)
    {
        await using var dbContext = CreateDbContext();
        var service = new StoryService(dbContext);
        var session = await SeedSessionAsync(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PatchSessionAsync(session.UserId, session.Id, new PatchSessionRequest(null, blank, null, null, null), CancellationToken.None));

        Assert.Equal("Genre cannot be blank.", exception.Message);
        dbContext.ChangeTracker.Clear();
        Assert.Equal("Superhero", (await dbContext.StorySessions.SingleAsync()).Genre);
    }

    [Fact]
    public async Task StoryCreation_DoesNotStartOpeningGenerationForInvalidRequest()
    {
        await using var dbContext = CreateDbContext();
        var sceneService = new Mock<ISceneService>(MockBehavior.Strict);
        var service = new StoryCreationService(new StoryService(dbContext), sceneService.Object, dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(Guid.NewGuid(), CreateRequestWithOversized("genre"), CancellationToken.None));

        sceneService.VerifyNoOtherCalls();
        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
    }

    private static CreateSessionRequest CreateRequestWithOversized(string field)
        => new(
            field == "title" ? MaxTitle + "t" : "Origin",
            field == "genre" ? MaxGenre + "g" : "Superhero",
            field == "archetype" ? MaxArchetype + "a" : "Guardian",
            field == "name" ? MaxHeroName + "n" : "Ari");

    private static async Task<StorySession> SeedSessionAsync(AppDbContext dbContext)
    {
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Origin",
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.StorySessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
