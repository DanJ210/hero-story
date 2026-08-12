using HeroStory.Core.Abstractions;

namespace HeroStory.Worker.Services;

public sealed class ImageGenerationWorker(
    ILogger<ImageGenerationWorker> logger,
    IImageJobQueue imageJobQueue,
    IStoryService storyService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Hero Story worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var storyId = await imageJobQueue.DequeueAsync(stoppingToken);
            if (storyId is Guid id)
            {
                logger.LogInformation("Processing story {StoryId}", id);
                await storyService.ProcessImagesAsync(id, stoppingToken);
                continue;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
