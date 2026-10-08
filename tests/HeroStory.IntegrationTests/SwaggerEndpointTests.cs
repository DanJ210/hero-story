using System.Net;
using System.Text.Json;

namespace HeroStory.IntegrationTests;

public class SwaggerEndpointTests
{
    [Fact]
    public async Task Swagger_InDevelopment_ServesUiAndDocument()
    {
        await using var fixture = new DevelopmentApiFixture();
        using var client = fixture.CreateClient();

        var uiResponse = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);

        var documentResponse = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);
        Assert.Equal("application/json", documentResponse.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await documentResponse.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("openapi", out _));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/sessions", out _));
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Swagger_InProduction_IsNotExposed(string path)
    {
        await using var fixture = new ProductionApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class ProductionApiFixture : ApiFixture
    {
        protected override string EnvironmentName => "Production";
    }
}
