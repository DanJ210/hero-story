namespace HeroStory.IntegrationTests.Scenes;

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
}
