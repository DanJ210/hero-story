using HeroStory.Core.Entities;
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
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (portrait is null)
        {
            return false;
        }

        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        await _blobService.DeleteAsync(containerName, portrait.BlobName, cancellationToken);
        portrait.DisabledAt = DateTime.UtcNow;
        portrait.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PortraitDto ToDto(UserPortrait portrait)
        => new(portrait.Id, portrait.ContentType, portrait.ContentLength, portrait.ConsentGrantedAt, portrait.CreatedAt);
}
