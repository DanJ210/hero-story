namespace HeroStory.IntegrationTests.Sessions;

using HeroStory.Api.DTOs.Auth;
using HeroStory.Api.DTOs.Session;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

public class SessionEndpointTests
{
    [Fact]
    public async Task GetWorkspace_ReturnsOwnedTurnsInOrderAndHidesForeignStory()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        Guid ownedSessionId;
        Guid foreignSessionId;
        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = dbContext.Users.Single(account => account.Email == "developer@hero-story.local");
            var ownedSession = CreateSession(user.Id, "Owned story");
            ownedSession.ContinuitySummary = "Internal continuity marker.";
            ownedSession.ContinuitySummaryThroughSequence = 12;
            ownedSession.ContinuitySummaryUpdatedAt = DateTime.UtcNow;
            var foreignSession = CreateSession(Guid.NewGuid(), "Foreign story");
            dbContext.AddRange(
                ownedSession,
                foreignSession,
                CreateScene(ownedSession.Id, 2, "Second passage"),
                CreateScene(ownedSession.Id, 1, "First passage"),
                CreateScene(foreignSession.Id, 1, "Hidden passage"));
            await dbContext.SaveChangesAsync();
            ownedSessionId = ownedSession.Id;
            foreignSessionId = foreignSession.Id;
        }

        var ownedResponse = await client.GetAsync($"/api/sessions/{ownedSessionId}/workspace");
        var workspaceJson = await ownedResponse.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var workspace = await ownedResponse.Content.ReadFromJsonAsync<StoryWorkspaceDto>(jsonOptions);
        var foreignResponse = await client.GetAsync($"/api/sessions/{foreignSessionId}/workspace");

        Assert.Equal(System.Net.HttpStatusCode.OK, ownedResponse.StatusCode);
        Assert.NotNull(workspace);
        Assert.Equal("Owned story", workspace.Session.Title);
        Assert.Equal([1, 2], workspace.Turns.Select(turn => turn.SequenceNumber));
        Assert.DoesNotContain("continuitySummary", workspaceJson);
        Assert.DoesNotContain("continuitySummaryThroughSequence", workspaceJson);
        Assert.DoesNotContain("continuitySummaryUpdatedAt", workspaceJson);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, foreignResponse.StatusCode);
    }

    [Fact]
    public async Task PauseAndResumeSession_TransitionsOwnedEpisodeStatus()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        StorySession session;
        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = dbContext.Users.Single(account => account.Email == "developer@hero-story.local");
            session = CreateSession(user.Id, "Pauseable story");
            dbContext.Add(session);
            await dbContext.SaveChangesAsync();
        }

        var pauseResponse = await client.PostAsync($"/api/sessions/{session.Id}/pause", null);
        var resumeResponse = await client.PostAsync($"/api/sessions/{session.Id}/resume", null);

        Assert.Equal(System.Net.HttpStatusCode.OK, pauseResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, resumeResponse.StatusCode);
        using var verificationScope = fixture.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(SessionStatus.Active, verificationContext.StorySessions.Single(story => story.Id == session.Id).Status);
    }

    [Fact]
    public async Task CreateSession_RejectsOversizedFieldsWithReadableErrorAndPersistsNothing()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = await CreateAuthenticatedClientAsync(fixture);

        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            title = "Origin",
            genre = new string('g', StorySessionFieldLimits.GenreMaxLength + 1),
            heroArchetype = new string('a', StorySessionFieldLimits.HeroArchetypeMaxLength + 1),
            heroName = "Ari"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Genre must be 500 characters or fewer. Hero archetype must be 1000 characters or fewer.",
            body.GetProperty("error").GetString());
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
        Assert.Empty(dbContext.Scenes.IgnoreQueryFilters());
    }

    [Fact]
    public async Task CreateSession_RejectsWhitespaceRequiredFieldAndPersistsNothing()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = await CreateAuthenticatedClientAsync(fixture);

        var response = await client.PostAsJsonAsync("/api/sessions", new
        {
            title = "Origin",
            genre = "   ",
            heroArchetype = "Guardian",
            heroName = "Ari"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(dbContext.StorySessions.IgnoreQueryFilters());
    }

    [Fact]
    public async Task PatchSession_RoundTripsMaximumLengthSetupText()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = await CreateAuthenticatedClientAsync(fixture);
        var sessionId = await SeedOwnedSessionAsync(fixture);
        var genre = string.Concat(Enumerable.Repeat("Mythic noir ", 50))[..StorySessionFieldLimits.GenreMaxLength];
        var archetype = string.Concat(Enumerable.Repeat("Reluctant guardian ", 60))[..StorySessionFieldLimits.HeroArchetypeMaxLength];

        var patchResponse = await client.PatchAsJsonAsync($"/api/sessions/{sessionId}", new { genre, heroArchetype = archetype });
        var getResponse = await client.GetAsync($"/api/sessions/{sessionId}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.OK, patchResponse.StatusCode);
        Assert.Equal(genre, fetched.GetProperty("genre").GetString());
        Assert.Equal(archetype, fetched.GetProperty("heroArchetype").GetString());
        Assert.Equal("Owned story", fetched.GetProperty("title").GetString());
        using var scope = fixture.Services.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<AppDbContext>().StorySessions.Single(story => story.Id == sessionId);
        Assert.Equal(genre, stored.Genre);
        Assert.Equal(archetype, stored.HeroArchetype);
    }

    [Fact]
    public async Task PatchSession_RejectsOversizedOrBlankFieldsWithoutChangingSession()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = await CreateAuthenticatedClientAsync(fixture);
        var sessionId = await SeedOwnedSessionAsync(fixture);

        var oversizedResponse = await client.PatchAsJsonAsync($"/api/sessions/{sessionId}", new
        {
            genre = new string('g', StorySessionFieldLimits.GenreMaxLength + 1),
            heroName = new string('n', StorySessionFieldLimits.HeroNameMaxLength + 1)
        });
        var oversizedBody = await oversizedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blankResponse = await client.PatchAsJsonAsync($"/api/sessions/{sessionId}", new { title = " " });
        var blankBody = await blankResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, oversizedResponse.StatusCode);
        Assert.Equal("Genre must be 500 characters or fewer. Hero name must be 100 characters or fewer.", oversizedBody.GetProperty("error").GetString());
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, blankResponse.StatusCode);
        Assert.Equal("Title cannot be blank.", blankBody.GetProperty("error").GetString());
        using var scope = fixture.Services.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<AppDbContext>().StorySessions.Single(story => story.Id == sessionId);
        Assert.Equal("Owned story", stored.Title);
        Assert.Equal("Superhero", stored.Genre);
        Assert.Equal("Ari", stored.HeroName);
    }

    private static async Task<System.Net.Http.HttpClient> CreateAuthenticatedClientAsync(DevelopmentApiFixture fixture)
    {
        var client = fixture.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private static async Task<Guid> SeedOwnedSessionAsync(DevelopmentApiFixture fixture)
    {
        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = dbContext.Users.Single(account => account.Email == "developer@hero-story.local");
        var session = CreateSession(user.Id, "Owned story");
        dbContext.Add(session);
        await dbContext.SaveChangesAsync();
        return session.Id;
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

    private static Scene CreateScene(Guid sessionId, int sequenceNumber, string narrative)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = sequenceNumber,
            ChoiceText = sequenceNumber == 1 ? "The story begins." : "Protect the city",
            NarrativeText = narrative,
            SceneSummary = $"Summary {sequenceNumber}",
            Location = "Lumina",
            ActiveConflict = "Protect the city",
            SuggestedActionsJson = "[\"Investigate\",\"Protect civilians\"]",
            StoryBeat = sequenceNumber == 1 ? StoryBeat.Opening : StoryBeat.Standard,
            ModerationStatus = ModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
