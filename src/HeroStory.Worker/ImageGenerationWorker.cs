using System.Text.Json;
using Azure.Storage.Queues.Models;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HeroStory.Worker;

public class ImageGenerationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AzureQueueClient _queueClient;
    private readonly WorkerOptions _options;
    private readonly ILogger<ImageGenerationWorker> _logger;
    private readonly IConfiguration _configuration;

    public ImageGenerationWorker(IServiceProvider serviceProvider, AzureQueueClient queueClient, IOptions<WorkerOptions> options, ILogger<ImageGenerationWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _queueClient = queueClient;
        _options = options.Value;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var messages = await _queueClient.DequeueAsync(32, stoppingToken);
            foreach (var message in messages)
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        if (message.DequeueCount > _options.MaxDequeueCount)
        {
            await _queueClient.MoveToPoisonAsync(message.MessageText, cancellationToken);
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var strategies = scope.ServiceProvider.GetRequiredService<IEnumerable<IImageGeneratorStrategy>>();
        var payload = JsonSerializer.Deserialize<JobPayload>(message.MessageText) ?? throw new InvalidOperationException("Queue payload invalid.");
        var job = await dbContext.GenerationJobs.SingleOrDefaultAsync(x => x.Id == payload.jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Skipping stale queue message for missing image job {JobId}.", payload.jobId);
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        if (job.Status == JobStatus.Completed)
        {
            _logger.LogInformation("Skipping already completed image job {JobId}.", job.Id);
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        job.Status = JobStatus.Processing;
        job.AttemptCount++;
        job.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var strategyName = _configuration["IMAGE_STRATEGY"] ?? "placeholder";
            var strategy = strategies.Single(x => string.Equals(x.Name, strategyName, StringComparison.OrdinalIgnoreCase));
            await strategy.GenerateAsync(job, cancellationToken);
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image generation failed for job {JobId}", job.Id);
            job.Status = message.DequeueCount >= _options.MaxDequeueCount ? JobStatus.Poisoned : JobStatus.Failed;
            job.ErrorDetail = ex is ArtworkPolicyException policyException
                ? $"{policyException.Code}: {policyException.Message}"
                : ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            if (message.DequeueCount >= _options.MaxDequeueCount)
            {
                await _queueClient.MoveToPoisonAsync(message.MessageText, cancellationToken);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            }
        }
    }

    private sealed record JobPayload(Guid jobId, Guid sceneId, Guid sessionId);
}
