namespace HeroStory.IntegrationTests.Sessions;

using HeroStory.Api.DTOs.Auth;
using HeroStory.Api.DTOs.Session;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
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
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var workspace = await ownedResponse.Content.ReadFromJsonAsync<StoryWorkspaceDto>(jsonOptions);
        var foreignResponse = await client.GetAsync($"/api/sessions/{foreignSessionId}/workspace");

        Assert.Equal(System.Net.HttpStatusCode.OK, ownedResponse.StatusCode);
        Assert.NotNull(workspace);
        Assert.Equal("Owned story", workspace.Session.Title);
        Assert.Equal([1, 2], workspace.Turns.Select(turn => turn.SequenceNumber));
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
