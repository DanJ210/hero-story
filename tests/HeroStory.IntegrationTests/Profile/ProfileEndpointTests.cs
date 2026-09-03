namespace HeroStory.IntegrationTests.Profile;

using System.Net.Http;
using HeroStory.Api.DTOs.Auth;
using HeroStory.Core.Entities;
using HeroStory.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

public class ProfileEndpointTests
{
    [Fact]
    public async Task DisablePortrait_DisablesActivePortraitAndSessionLikenessOptIn()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var userId = await AuthenticateDevelopmentUserAsync(fixture, client);
        var portrait = CreatePortrait(userId);
        var session = CreateSession(userId, likenessEnabled: true);

        using (var scope = fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.AddRange(portrait, session);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsync("/api/profile/portrait/disable", null);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        using var verificationScope = fixture.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedPortrait = verificationContext.UserPortraits.Single(candidate => candidate.Id == portrait.Id);
        var storedSession = verificationContext.StorySessions.Single(candidate => candidate.Id == session.Id);
        Assert.NotNull(storedPortrait.DisabledAt);
        Assert.Null(storedPortrait.DeletedAt);
        Assert.False(storedSession.LikenessEnabled);

        var getResponse = await client.GetAsync("/api/profile/portrait");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
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

    private static UserPortrait CreatePortrait(Guid userId)
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        return new UserPortrait
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlobName = "users/test/portraits/portrait-a",
            ContentType = "image/jpeg",
            ContentLength = 2048,
            ConsentGrantedAt = createdAt,
            CreatedAt = createdAt
        };
    }

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
}