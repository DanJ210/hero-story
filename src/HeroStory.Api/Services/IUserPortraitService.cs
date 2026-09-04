namespace HeroStory.Api.Services;

public interface IUserPortraitService
{
    Task<PortraitDto> UploadAsync(Guid userId, Stream content, string contentType, long contentLength, bool consentGranted, CancellationToken cancellationToken);
    Task<bool> DisableAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
    Task<PortraitPurgeResult> PurgeAsync(Guid userId, CancellationToken cancellationToken);
    Task<PortraitDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserPortraitReference?> GetActiveReferenceAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record PortraitDto(Guid Id, string ContentType, long ContentLength, DateTime ConsentGrantedAt, DateTime CreatedAt, string ThumbnailUrl);
public sealed record UserPortraitReference(Guid Id, DateTime ConsentGrantedAt);
public sealed record PortraitPurgeResult(int PortraitsDeleted, int BlobsRemoved, int JobsSettled);
