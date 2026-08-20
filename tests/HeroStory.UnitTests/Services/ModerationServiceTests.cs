using HeroStory.Api.Services;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HeroStory.UnitTests.Services;

public class ModerationServiceTests
{
    [Fact]
    public async Task ModerateInputAsync_RejectsKeyword()
    {
        var client = new Mock<OpenAiClient>(new HttpClient(), new ConfigurationBuilder().AddInMemoryCollection().Build());
var keywordPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-keywords-test.txt");
try
{
    await File.WriteAllTextAsync(keywordPath, "forbidden");
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["MODERATION_KEYWORD_LIST_PATH"] = keywordPath
    }).Build();

    var service = new ModerationService(client.Object, config);
    var result = await service.ModerateInputAsync("A forbidden spell", CancellationToken.None);

    Assert.Equal(ModerationStatus.Rejected, result.Status);
}
finally
{
    if (File.Exists(keywordPath)) File.Delete(keywordPath);
}
    }

    [Fact]
    public async Task ModerateOutputAsync_KeepsNarrativeWhenOnlyGenreViolenceIsFlagged()
    {
        var narrative = "Danakin ignited his lightsaber and struck down the assassin.";
        var service = new ModerationService(CreateClient(["violence"]).Object, EmptyConfiguration());

        var result = await service.ModerateOutputAsync(narrative, CancellationToken.None);

        Assert.Equal(ModerationStatus.Approved, result.Status);
        Assert.Equal(narrative, result.Narrative);
        Assert.Null(result.Detail);
    }

    [Fact]
    public async Task ModerateOutputAsync_SanitizesUnsafeCategory()
    {
        var service = new ModerationService(CreateClient(["violence", "sexual/minors"]).Object, EmptyConfiguration());

        var result = await service.ModerateOutputAsync("unsafe passage", CancellationToken.None);

        Assert.Equal(ModerationStatus.Sanitized, result.Status);
        Assert.Equal("The hero pauses, choosing a safer path forward.", result.Narrative);
        Assert.Contains("sexual/minors", result.Detail);
        Assert.DoesNotContain("violence,", result.Detail);
    }

    [Fact]
    public async Task ModerateInputAsync_AllowsGenreViolenceAction()
    {
        var service = new ModerationService(CreateClient(["violence"]).Object, EmptyConfiguration());

        var result = await service.ModerateInputAsync("I strike the villain with my lightsaber", CancellationToken.None);

        Assert.Equal(ModerationStatus.Approved, result.Status);
    }

    [Fact]
    public async Task ModerateOutputAsync_HonoursConfiguredBlockedCategories()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MODERATION_BLOCKED_CATEGORIES"] = "violence, sexual/minors"
        }).Build();
        var service = new ModerationService(CreateClient(["violence"]).Object, configuration);

        var result = await service.ModerateOutputAsync("a battle scene", CancellationToken.None);

        Assert.Equal(ModerationStatus.Sanitized, result.Status);
    }

    private static IConfiguration EmptyConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static Mock<OpenAiClient> CreateClient(string[] flaggedCategories)
    {
        var client = new Mock<OpenAiClient>(new HttpClient(), EmptyConfiguration());
        client.Setup(openAi => openAi.GetFlaggedCategoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(flaggedCategories);
        return client;
    }
}
