using global::HeroStory.Api.Services;
using global::HeroStory.Infrastructure.Clients;
using global::HeroStory.Infrastructure.Data;
using global::HeroStory.Infrastructure.Storage;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace HeroStory.Worker;

public class OpenAiImageStrategy : IImageGeneratorStrategy
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly AppDbContext _dbContext;
    private readonly OpenAiClient _openAiClient;
    private readonly AzureBlobService _blobService;
    private readonly IConfiguration _configuration;

    public OpenAiImageStrategy(IBlobStorageService blobStorageService, AppDbContext dbContext, OpenAiClient openAiClient, AzureBlobService blobService, IConfiguration configuration)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
        _openAiClient = openAiClient;
        _blobService = blobService;
        _configuration = configuration;
    }

    public string Name => "openai";

    public async Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        var ownerUserId = Guid.Empty;
        Scene? scene = null;
        PortraitAuditEvent? providerUseStartedAudit = null;
        string? uploadedBlobName = null;
        try
        {
            scene = await _dbContext.Scenes
                .Include(s => s.Session)
                .SingleAsync(x => x.Id == job.SceneId, cancellationToken);
            ownerUserId = scene.Session.UserId;

            var imagePrompt = GenerateImagePrompt(scene);
            byte[] imageBytes;
            if (job.PortraitId is not null)
            {
                var maxReferenceAge = PortraitLikenessPolicy.ResolveReferenceMaxAge(_configuration);
                var (portrait, consentRecord) = await LoadAndValidatePortraitAsync(job, scene.Session.UserId, maxReferenceAge, cancellationToken);
                var portraitsContainer = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
                await using var portraitStream = await _blobService.DownloadAsync(portraitsContainer, portrait.BlobName, cancellationToken);
                await using var normalizedPortrait = await NormalizePortraitAsync(portraitStream, cancellationToken);

                (portrait, consentRecord) = await LoadAndValidatePortraitAsync(job, scene.Session.UserId, maxReferenceAge, cancellationToken);
                providerUseStartedAudit = new PortraitAuditEvent
                {
                    Id = Guid.NewGuid(),
                    SubjectUserId = scene.Session.UserId,
                    ActorType = "worker",
                    EventType = "likeness_provider_use_started",
                    PortraitId = portrait.Id,
                    ConsentRecordId = consentRecord.Id,
                    SessionId = scene.SessionId,
                    SceneId = scene.Id,
                    GenerationJobId = job.Id,
                    OccurredAt = DateTime.UtcNow
                };
                _dbContext.PortraitAuditEvents.Add(providerUseStartedAudit);
                _dbContext.Entry(job).Property(candidate => candidate.Status).IsModified = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                imageBytes = await _openAiClient.GenerateImageWithReferenceAsync(imagePrompt, normalizedPortrait, "image/jpeg", cancellationToken);
                await LoadAndValidatePortraitAsync(job, scene.Session.UserId, maxReferenceAge, cancellationToken);
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
            var blobName = $"scenes/{job.SceneId}/generated/{job.Id}.png";
            await _blobStorageService.UploadPlaceholderAsync(blobName, stream, "image/png", cancellationToken);
            uploadedBlobName = blobName;
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
            if (providerUseStartedAudit is not null
                && _dbContext.Entry(providerUseStartedAudit).State == EntityState.Added)
            {
                _dbContext.Entry(providerUseStartedAudit).State = EntityState.Detached;
            }

            await _dbContext.Entry(job).ReloadAsync(cancellationToken);
            if (ex is DbUpdateConcurrencyException && scene is not null)
            {
                await _dbContext.Entry(scene).ReloadAsync(cancellationToken);
            }

            var policyException = ex as ArtworkPolicyException;
            if (job.PortraitId is not null && (policyException is not null || job.Status == Core.Enums.JobStatus.Poisoned))
            {
                _dbContext.PortraitAuditEvents.Add(new PortraitAuditEvent
                {
                    Id = Guid.NewGuid(),
                    SubjectUserId = ownerUserId,
                    ActorType = "worker",
                    EventType = "likeness_use_rejected",
                    PortraitId = job.PortraitId,
                    ConsentRecordId = job.PortraitConsentRecordId,
                    SessionId = job.SessionId,
                    SceneId = job.SceneId,
                    GenerationJobId = job.Id,
                    DetailCode = policyException?.Code ?? ArtworkErrorCode.PortraitConsentMissing,
                    OccurredAt = DateTime.UtcNow
                });
            }

            if (job.Status is not (Core.Enums.JobStatus.Poisoned or Core.Enums.JobStatus.Completed))
            {
                job.Status = Core.Enums.JobStatus.Failed;
                job.ErrorDetail = policyException is not null
                    ? $"{policyException.Code}: {policyException.Message}"
                    : ex.Message;
                job.UpdatedAt = DateTime.UtcNow;
            }
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _dbContext.Entry(job).ReloadAsync(cancellationToken);
                if (scene is not null)
                {
                    await _dbContext.Entry(scene).ReloadAsync(cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            if (uploadedBlobName is not null)
            {
                await _blobStorageService.DeleteImageAsync(uploadedBlobName, cancellationToken);
            }

            throw;
        }
    }

    private async Task<(UserPortrait Portrait, PortraitConsentRecord Consent)> LoadAndValidatePortraitAsync(
        GenerationJob job,
        Guid userId,
        TimeSpan maxReferenceAge,
        CancellationToken cancellationToken)
    {
        await _dbContext.Entry(job).ReloadAsync(cancellationToken);
        var portrait = await _dbContext.UserPortraits
            .AsNoTracking()
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArtworkPolicyException(ArtworkErrorCode.PortraitUnavailable, "The consented portrait is no longer available.");

        if (job.Status == Core.Enums.JobStatus.Poisoned)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitConsentMissing, "Likeness consent was revoked before provider use.");
        }

        var consentRecord = job.PortraitConsentRecordId is Guid consentRecordId
            ? await _dbContext.PortraitConsentRecords.AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == consentRecordId, cancellationToken)
            : null;
        var consentRevoked = consentRecord is not null && await _dbContext.PortraitAuditEvents
            .AsNoTracking()
            .AnyAsync(audit => audit.ConsentRecordId == consentRecord.Id && audit.EventType == "consent_revoked", cancellationToken);

        PortraitLikenessPolicy.ValidateForGeneration(
            job, portrait, consentRecord, userId, consentRevoked, DateTime.UtcNow, maxReferenceAge);
        return (portrait, consentRecord!);
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

    private static async Task<MemoryStream> NormalizePortraitAsync(Stream source, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(source, cancellationToken);
        var normalized = new MemoryStream();
        await image.SaveAsJpegAsync(normalized, new JpegEncoder { Quality = 92 }, cancellationToken);
        normalized.Position = 0;
        return normalized;
    }

}
