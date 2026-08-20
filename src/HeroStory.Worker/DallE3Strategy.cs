using global::HeroStory.Api.Services;
using global::HeroStory.Infrastructure.Clients;
using global::HeroStory.Infrastructure.Data;
using global::HeroStory.Infrastructure.Storage;
using HeroStory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Worker;

public class DallE3Strategy : IImageGeneratorStrategy
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly AppDbContext _dbContext;
    private readonly OpenAiClient _openAiClient;
    private readonly AzureBlobService _blobService;
    private readonly IConfiguration _configuration;

    public DallE3Strategy(IBlobStorageService blobStorageService, AppDbContext dbContext, OpenAiClient openAiClient, AzureBlobService blobService, IConfiguration configuration)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
        _openAiClient = openAiClient;
        _blobService = blobService;
        _configuration = configuration;
    }

    public string Name => "dalle3";

    public async Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        try
        {
            var scene = await _dbContext.Scenes
                .Include(s => s.Session)
                .SingleAsync(x => x.Id == job.SceneId, cancellationToken);

            var imagePrompt = GenerateImagePrompt(scene);
            byte[] imageBytes;
            if (job.PortraitId is Guid portraitId)
            {
                var portrait = await _dbContext.UserPortraits.SingleOrDefaultAsync(
                    candidate => candidate.Id == portraitId
                        && candidate.UserId == scene.Session.UserId
                        && candidate.DeletedAt == null
                        && candidate.DisabledAt == null,
                    cancellationToken)
                    ?? throw new InvalidOperationException("The consented portrait is no longer available.");
                var portraitsContainer = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
                await using var portraitStream = await _blobService.DownloadAsync(portraitsContainer, portrait.BlobName, cancellationToken);
                imageBytes = await _openAiClient.GenerateImageWithReferenceAsync(imagePrompt, portraitStream, portrait.ContentType, cancellationToken);
            }
            else
            {
                imageBytes = await _openAiClient.GenerateImageAsync(imagePrompt, cancellationToken);
            }

            await _dbContext.Entry(scene).ReloadAsync(cancellationToken);
            if (!scene.IsActive)
            {
                await CompleteSupersededJobAsync(job, cancellationToken);
                return;
            }

            await using var stream = new MemoryStream(imageBytes);
            var blobName = $"scenes/{job.SceneId}/generated.png";
            await _blobStorageService.UploadPlaceholderAsync(blobName, stream, "image/png", cancellationToken);
            var signedUrl = _blobStorageService.GenerateImageAccessUrl(blobName);

            scene.ImageUrl = signedUrl;
            scene.ImageUrlExpiresAt = DateTime.UtcNow.AddHours(24);
            scene.UpdatedAt = DateTime.UtcNow;
            job.Status = Core.Enums.JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = Core.Enums.JobStatus.Failed;
            job.ErrorDetail = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task CompleteSupersededJobAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        job.Status = Core.Enums.JobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GenerateImagePrompt(Scene scene)
    {
        var heroName = scene.Session.HeroName ?? "the hero";
        var location = !string.IsNullOrWhiteSpace(scene.Location) ? scene.Location : "an epic location";
        var conflict = !string.IsNullOrWhiteSpace(scene.ActiveConflict) ? scene.ActiveConflict : "facing a challenge";
        var summary = !string.IsNullOrWhiteSpace(scene.SceneSummary) ? scene.SceneSummary : "in the story";

        return $@"Create a vivid, epic fantasy illustration for a story scene:

Hero: {heroName}
Location: {location}
Action/Conflict: {conflict}
Scene Summary: {summary}

Style: Book cover quality, cinematic lighting, rich colors, detailed environment, fantasy artwork. Show {heroName} as the central figure in the scene, engaged in the action described. Make it feel like a moment from an epic fantasy novel.

Do not include text, names, or dialogue overlays.";
    }

}
