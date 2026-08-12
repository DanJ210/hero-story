using HeroStory.Infrastructure.Storage;

namespace HeroStory.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadPlaceholderAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    string GenerateImageAccessUrl(string blobName);
    Task DeleteImageAsync(string blobName, CancellationToken cancellationToken);
}
