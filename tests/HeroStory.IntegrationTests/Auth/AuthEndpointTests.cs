namespace HeroStory.IntegrationTests.Auth;

public class AuthEndpointTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public AuthEndpointTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_ReturnsCreated()
    {
        using var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "hero@example.com", password = "Password1", displayName = "Hero" });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
}
