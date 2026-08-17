namespace HeroStory.IntegrationTests.Scenes;

using System.Net.Http;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

public class SceneEndpointTests
{
    [Fact]
    public async Task GetScene_SerializesEnumContractsAsCamelCaseStrings()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        Guid sessionId;
        Guid sceneId;
        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = dbContext.Users.Single(account => account.Email == "developer@hero-story.local");
            var session = new StorySession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
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
                ChoiceText = "Act",
                NarrativeText = "Narrative",
                SceneSummary = "Summary",
                Location = "Location",
                ActiveConflict = "Conflict",
                StoryBeat = StoryBeat.Major,
                ModerationStatus = ModerationStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            sessionId = session.Id;
            sceneId = scene.Id;
            dbContext.AddRange(session, scene);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/sessions/{sessionId}/scenes/{sceneId}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"storyBeat\":\"major\"", json);
        Assert.Contains("\"artworkStatus\":\"notRequested\"", json);
    }

    [Fact]
    public async Task GetScenes_ExcludesSupersededTurnsFromActivePath()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var userId = await AuthenticateDevelopmentUserAsync(fixture, client);
        var session = CreateSession(userId);
        var activeScene = CreateScene(session.Id, 1, "Active path", true);
        var supersededScene = CreateScene(session.Id, 2, "Superseded path", false);

        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AddRange(session, activeScene, supersededScene);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/sessions/{session.Id}/scenes");
        var scenes = await response.Content.ReadFromJsonAsync<SceneListResponse[]>();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(scenes);
        Assert.Single(scenes);
        Assert.Equal(activeScene.Id, scenes[0].Id);
    }

    [Fact]
    public async Task ReviseScene_ReturnsNotFoundForAnotherUsersScene()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        await AuthenticateDevelopmentUserAsync(fixture, client);
        var otherUserId = Guid.NewGuid();
        var session = CreateSession(otherUserId);
        var scene = CreateScene(session.Id, 1, "Other user scene", true);

        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AddRange(session, scene);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/scenes/{scene.Id}/revisions",
            new { choiceText = "Take a different path" });

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestArtwork_QueuesOwnedSceneAndRejectsDuplicatePendingRequest()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var userId = await AuthenticateDevelopmentUserAsync(fixture, client);
        var session = CreateSession(userId);
        var scene = CreateScene(session.Id, 1, "Manual artwork", true);

        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AddRange(session, scene);
            await dbContext.SaveChangesAsync();
        }

        var firstResponse = await client.PostAsync($"/api/sessions/{session.Id}/scenes/{scene.Id}/artwork", null);
        var duplicateResponse = await client.PostAsync($"/api/sessions/{session.Id}/scenes/{scene.Id}/artwork", null);

        Assert.Equal(System.Net.HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        using var verificationScope = fixture.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(verificationContext.GenerationJobs.Where(job => job.SceneId == scene.Id));
        Assert.Equal(JobStatus.Queued, verificationContext.GenerationJobs.Single(job => job.SceneId == scene.Id).Status);
    }

    private static async Task<Guid> AuthenticateDevelopmentUserAsync(DevelopmentApiFixture fixture, HttpClient client)
    {
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return dbContext.Users.Single(user => user.Email == "developer@hero-story.local").Id;
    }

    private static StorySession CreateSession(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Story",
            Genre = "Superhero",
            HeroArchetype = "Guardian",
            HeroName = "Ari",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static Scene CreateScene(Guid sessionId, int sequenceNumber, string narrativeText, bool isActive)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = sequenceNumber,
            IsActive = isActive,
            ChoiceText = "Act",
            NarrativeText = narrativeText,
            SceneSummary = "Summary",
            Location = "Location",
            ActiveConflict = "Conflict",
            ModerationStatus = ModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private sealed record SceneListResponse(Guid Id);
}
