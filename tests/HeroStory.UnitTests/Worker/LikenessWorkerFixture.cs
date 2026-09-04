using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace HeroStory.UnitTests.Worker;

/// <summary>
/// Stands in for the OpenAI transport so no image call can leave the test process.
/// Recording the requests is what lets a test prove a policy violation failed closed.
/// </summary>
internal sealed class RecordingOpenAiHandler : HttpMessageHandler
{
    private readonly Func<Task>? _onRequest;

    public RecordingOpenAiHandler(Func<Task>? onRequest = null) => _onRequest = onRequest;

    public List<string> RequestedPaths { get; } = [];

    public bool ImageCallMade => RequestedPaths.Any(path => path.Contains("/images/", StringComparison.Ordinal));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedPaths.Add(request.RequestUri!.AbsolutePath);
        if (_onRequest is not null)
        {
            await _onRequest();
        }

        var body = JsonSerializer.Serialize(new
        {
            data = new[] { new { b64_json = Convert.ToBase64String(LikenessWorkerFixture.GeneratedImageBytes) } }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}

internal static class LikenessWorkerFixture
{
    public const string PortraitsContainer = "test-portraits";

    public static byte[] GeneratedImageBytes { get; } = [0x89, 0x50, 0x4E, 0x47];

    public static AppDbContext CreateDbContext(string databaseName)
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName).Options);

    public static IConfiguration CreateConfiguration(int referenceMaxAgeMinutes = 120)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AZURE_BLOB_PORTRAITS_CONTAINER"] = PortraitsContainer,
            ["LIKENESS_PROVIDER_REFERENCE_MAX_AGE_MINUTES"] = referenceMaxAgeMinutes.ToString(),
            ["AZURE_BLOB_CONNECTION_STRING"] = "UseDevelopmentStorage=true"
        }).Build();

    public static OpenAiClient CreateOpenAiClient(RecordingOpenAiHandler handler, IConfiguration configuration)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://openai.test") }, configuration);

    /// <summary>Portrait bytes must be a decodable image because the strategy re-encodes before sending.</summary>
    public static Mock<AzureBlobService> CreatePortraitBlobService(IConfiguration configuration)
    {
        var blobService = new Mock<AzureBlobService>(configuration) { CallBase = false };
        blobService
            .Setup(service => service.DownloadAsync(PortraitsContainer, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateJpegStream());
        return blobService;
    }

    private static Stream CreateJpegStream()
    {
        using var image = new Image<Rgba32>(8, 8);
        var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder { Quality = 80 });
        stream.Position = 0;
        return stream;
    }

    public static StorySession CreateSession(Guid userId, bool likenessEnabled = true)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HeroName = "Test Hero",
            LikenessEnabled = likenessEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static Scene CreateScene(Guid sessionId, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SequenceNumber = 1,
            IsActive = isActive,
            NarrativeText = "The hero stands ready.",
            SceneSummary = "Opening moment",
            Location = "The rooftop",
            ActiveConflict = "A siren rises",
            StoryBeat = StoryBeat.Opening,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static UserPortrait CreatePortrait(Guid userId, DateTime consentGrantedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlobName = $"users/{userId}/portraits/{Guid.NewGuid()}",
            ContentType = "image/jpeg",
            ContentLength = 2048,
            ConsentGrantedAt = consentGrantedAt,
            CreatedAt = consentGrantedAt
        };

    public static GenerationJob CreateLikenessJob(Scene scene, UserPortrait? portrait, DateTime createdAt, JobStatus status = JobStatus.Processing)
        => new()
        {
            Id = Guid.NewGuid(),
            SceneId = scene.Id,
            SessionId = scene.SessionId,
            PortraitId = portrait?.Id,
            PortraitConsentGrantedAt = portrait?.ConsentGrantedAt,
            Prompt = "Illustrate the opening beat.",
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
}
