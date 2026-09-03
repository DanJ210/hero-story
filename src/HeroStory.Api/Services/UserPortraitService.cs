using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class UserPortraitService : IUserPortraitService
{
    private const long MaximumPortraitBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly AppDbContext _dbContext;
    private readonly AzureBlobService _blobService;
    private readonly IConfiguration _configuration;

    public UserPortraitService(AppDbContext dbContext, AzureBlobService blobService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _blobService = blobService;
        _configuration = configuration;
    }

    public async Task<PortraitDto> UploadAsync(Guid userId, Stream content, string contentType, long contentLength, bool consentGranted, CancellationToken cancellationToken)
    {
        if (!consentGranted)
        {
            throw new InvalidOperationException("Explicit portrait consent is required.");
        }
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Portrait must be a JPEG, PNG, or WebP image.");
        }
        if (contentLength <= 0 || contentLength > MaximumPortraitBytes)
        {
            throw new InvalidOperationException("Portrait must be between 1 byte and 10 MB.");
        }

        var activePortraits = await _dbContext.UserPortraits
            .Where(portrait => portrait.UserId == userId && portrait.DeletedAt == null && portrait.DisabledAt == null)
            .ToListAsync(cancellationToken);
        foreach (var activePortrait in activePortraits)
        {
            activePortrait.DisabledAt = DateTime.UtcNow;
        }

        var portrait = new UserPortrait
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlobName = $"users/{userId}/portraits/{Guid.NewGuid():N}",
            ContentType = contentType,
            ContentLength = contentLength,
            ConsentGrantedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        await _blobService.UploadAsync(containerName, portrait.BlobName, content, contentType, cancellationToken);
        _dbContext.UserPortraits.Add(portrait);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(portrait);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var hasPortrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null)
            .AnyAsync(cancellationToken);
        if (!hasPortrait)
        {
            return false;
        }

        await PurgeAsync(userId, cancellationToken);
        return true;
    }

    public async Task<PortraitPurgeResult> PurgeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portraits = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ToListAsync(cancellationToken);

        var blobNames = portraits
            .Select(portrait => portrait.BlobName)
            .Where(blobName => !string.IsNullOrWhiteSpace(blobName))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var now = DateTime.UtcNow;
        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        foreach (var blobName in blobNames)
        {
            await _blobService.DeleteAsync(containerName, blobName, cancellationToken);
        }

        foreach (var portrait in portraits)
        {
            portrait.DisabledAt ??= now;
            portrait.DeletedAt = now;
        }

        var userSessionIds = await _dbContext.StorySessions
            .Where(session => session.UserId == userId)
            .Select(session => session.Id)
            .ToListAsync(cancellationToken);

        var outstandingJobs = await _dbContext.GenerationJobs
            .Where(job => job.PortraitId.HasValue
                && userSessionIds.Contains(job.SessionId)
                && (job.Status == JobStatus.Queued || job.Status == JobStatus.Processing || job.Status == JobStatus.Failed))
            .ToListAsync(cancellationToken);
        foreach (var job in outstandingJobs)
        {
            job.Status = JobStatus.Poisoned;
            job.CompletedAt ??= now;
            job.ErrorDetail = "PortraitDeleted: Likeness source was deleted before artwork generation completed.";
            job.UpdatedAt = now;
        }

        await DisableSessionLikenessAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PortraitPurgeResult(portraits.Count, blobNames.Count, outstandingJobs.Count);
    }

    public async Task<bool> DisableAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activePortraits = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .ToListAsync(cancellationToken);
        if (activePortraits.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var portrait in activePortraits)
        {
            portrait.DisabledAt = now;
        }

        await DisableSessionLikenessAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserPortraitReference?> GetActiveReferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return portrait is null ? null : new UserPortraitReference(portrait.Id, portrait.ConsentGrantedAt);
    }

    public async Task<PortraitDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return portrait is null ? null : ToDto(portrait);
    }

    private PortraitDto ToDto(UserPortrait portrait)
    {
        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        var thumbnailUrl = _blobService.GenerateSasUrl(containerName, portrait.BlobName);
        return new PortraitDto(portrait.Id, portrait.ContentType, portrait.ContentLength, portrait.ConsentGrantedAt, portrait.CreatedAt, thumbnailUrl);
    }

    private async Task DisableSessionLikenessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var optedInSessions = await _dbContext.StorySessions
            .Where(session => session.UserId == userId && session.LikenessEnabled)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var session in optedInSessions)
        {
            session.LikenessEnabled = false;
            session.UpdatedAt = now;
        }
    }
}
