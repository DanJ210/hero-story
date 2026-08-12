using HeroStory.Infrastructure.Storage;

namespace HeroStory.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly AzureBlobService _blobService;
    private readonly IConfiguration _configuration;
    private readonly string _containerName;

    public BlobStorageService(AzureBlobService blobService, IConfiguration configuration)
    {
        _blobService = blobService;
        _configuration = configuration;
        _containerName = _configuration["AZURE_BLOB_IMAGES_CONTAINER"] ?? "images";
    }

    public Task<string> UploadImageAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
        => _blobService.UploadAsync(_containerName, blobName, content, contentType, cancellationToken);

    public string GenerateImageUrl(string blobName)
        => _blobService.GenerateSasUrl(_containerName, blobName);

    public Task DeleteImageAsync(string blobName, CancellationToken cancellationToken)
        => _blobService.DeleteAsync(_containerName, blobName, cancellationToken);

    public Task<string> UploadPlaceholderAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken)
        => UploadImageAsync(blobName, content, contentType, cancellationToken);

    public string GenerateImageAccessUrl(string blobName)
        => GenerateImageUrl(blobName);
}
