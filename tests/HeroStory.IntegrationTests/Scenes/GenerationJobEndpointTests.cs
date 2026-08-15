using HeroStory.Api.DTOs.Auth;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HeroStory.IntegrationTests.Scenes;

public class GenerationJobEndpointTests
{
    [Fact]
    public async Task GetJob_EnforcesStoryOwnership()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/dev-login", null);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        Guid ownedJobId;
        Guid foreignJobId;
        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = dbContext.Users.Single(account => account.Email == "developer@hero-story.local");
            var ownedSession = CreateSession(user.Id);
            var foreignSession = CreateSession(Guid.NewGuid());
            var ownedScene = CreateScene(ownedSession.Id);
            var foreignScene = CreateScene(foreignSession.Id);
            var ownedJob = CreateJob(ownedSession.Id, ownedScene.Id);
            var foreignJob = CreateJob(foreignSession.Id, foreignScene.Id);
            ownedJobId = ownedJob.Id;
            foreignJobId = foreignJob.Id;
            dbContext.AddRange(ownedSession, foreignSession, ownedScene, foreignScene, ownedJob, foreignJob);
            await dbContext.SaveChangesAsync();
        }

        var ownedResponse = await client.GetAsync($"/api/jobs/{ownedJobId}");
        var foreignResponse = await client.GetAsync($"/api/jobs/{foreignJobId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, ownedResponse.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, foreignResponse.StatusCode);
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

    private static Scene CreateScene(Guid sessionId)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = 1,
            ChoiceText = "Act",
            NarrativeText = "Narrative",
            SceneSummary = "Summary",
            Location = "Location",
            ActiveConflict = "Conflict",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static GenerationJob CreateJob(Guid sessionId, Guid sceneId)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SceneId = sceneId,
            Prompt = "Prompt",
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}