using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Infrastructure.Storage;

public class AzureBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;

    public AzureBlobService(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = _configuration["AZURE_BLOB_CONNECTION_STRING"] ?? "UseDevelopmentStorage=true";
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
        await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public string GenerateSasUrl(string containerName, string blobName)
    {
        var expiryHours = int.TryParse(_configuration["AZURE_BLOB_SAS_EXPIRY_HOURS"], out var parsed) ? parsed : 24;
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        if (!blobClient.CanGenerateSasUri)
        {
            return blobClient.Uri.ToString();
        }

        var sas = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        return blobClient.GenerateSasUri(sas).ToString();
    }

    public Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
