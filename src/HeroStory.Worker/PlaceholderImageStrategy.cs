using HeroStory.Api.Services;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Worker;

public class PlaceholderImageStrategy : IImageGeneratorStrategy
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly AppDbContext _dbContext;

    public PlaceholderImageStrategy(IBlobStorageService blobStorageService, AppDbContext dbContext)
    {
        _blobStorageService = blobStorageService;
        _dbContext = dbContext;
    }

    public string Name => "placeholder";

    public async Task GenerateAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        var placeholderPath = Path.Combine(AppContext.BaseDirectory, "assets", "placeholder.png");
        await using var stream = File.OpenRead(placeholderPath);
        var blobName = $"scenes/{job.SceneId}/placeholder.png";
        var url = await _blobStorageService.UploadPlaceholderAsync(blobName, stream, "image/png", cancellationToken);
        var scene = await _dbContext.Scenes.SingleAsync(x => x.Id == job.SceneId, cancellationToken);
        scene.ImageUrl = url;
        scene.ImageUrlExpiresAt = DateTime.UtcNow.AddHours(24);
        scene.UpdatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
