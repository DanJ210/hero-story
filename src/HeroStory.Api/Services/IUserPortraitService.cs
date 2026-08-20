namespace HeroStory.Api.Services;

public interface IUserPortraitService
{
    Task<PortraitDto> UploadAsync(Guid userId, Stream content, string contentType, long contentLength, bool consentGranted, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record PortraitDto(Guid Id, string ContentType, long ContentLength, DateTime ConsentGrantedAt, DateTime CreatedAt);
