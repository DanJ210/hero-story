namespace HeroStory.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    string GenerateImageUrl(string blobName);
    Task DeleteImageAsync(string blobName, CancellationToken cancellationToken);
    Task<string> UploadPlaceholderAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken);
    string GenerateImageAccessUrl(string blobName);
}
