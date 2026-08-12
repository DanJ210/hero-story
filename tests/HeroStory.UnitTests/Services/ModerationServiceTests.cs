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
        var keywordPath = Path.Combine(Directory.GetCurrentDirectory(), "keywords-test.txt");
        await File.WriteAllTextAsync(keywordPath, "forbidden");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MODERATION_KEYWORD_LIST_PATH"] = keywordPath }).Build();

        var service = new ModerationService(client.Object, config);
        var result = await service.ModerateInputAsync("A forbidden spell", CancellationToken.None);

        Assert.Equal(ModerationStatus.Rejected, result.Status);
        File.Delete(keywordPath);
    }
}
