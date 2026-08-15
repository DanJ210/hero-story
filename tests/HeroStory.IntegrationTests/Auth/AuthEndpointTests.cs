namespace HeroStory.IntegrationTests.Auth;

using HeroStory.Api.DTOs.Auth;

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

    [Fact]
    public async Task DevelopmentLogin_IsNotMappedOutsideDevelopment()
    {
        using var client = _fixture.CreateClient();
        var response = await client.PostAsync("/api/auth/dev-login", null);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentLogin_ReturnsTokensInDevelopment()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();
        var response = await client.PostAsync("/api/auth/dev-login", null);
        var responseBody = await response.Content.ReadAsStringAsync();
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.True(response.IsSuccessStatusCode, $"Expected development login to succeed, but received {(int)response.StatusCode}: {responseBody}");
        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var protectedResponse = await client.GetAsync("/api/sessions");
        Assert.Equal(System.Net.HttpStatusCode.OK, protectedResponse.StatusCode);
    }
}
