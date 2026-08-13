using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Infrastructure.Clients;

public class AzureQueueClient
{
    private readonly QueueClient _queueClient;
    private readonly QueueClient _poisonQueueClient;
    private readonly IConfiguration _configuration;

    public AzureQueueClient(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = _configuration["AZURE_QUEUE_CONNECTION_STRING"] ?? "UseDevelopmentStorage=true";
        var queueName = _configuration["AZURE_QUEUE_IMAGE_JOBS_NAME"] ?? "image-generation-jobs";
        var poisonQueueName = _configuration["AZURE_QUEUE_POISON_NAME"] ?? "image-generation-jobs-poison";
        _queueClient = new QueueClient(connectionString, queueName);
        _poisonQueueClient = new QueueClient(connectionString, poisonQueueName);
        _queueClient.CreateIfNotExists();
        _poisonQueueClient.CreateIfNotExists();
    }

    public virtual Task EnqueueAsync(string message, CancellationToken cancellationToken)
        => _queueClient.SendMessageAsync(message, cancellationToken: cancellationToken);

    public virtual async Task<IReadOnlyList<QueueMessage>> DequeueAsync(int maxMessages, CancellationToken cancellationToken)
    {
        var visibilityTimeout = int.TryParse(_configuration["AZURE_QUEUE_VISIBILITY_TIMEOUT_SECONDS"], out var parsed) ? parsed : 30;
        var result = await _queueClient.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(visibilityTimeout), cancellationToken);
        return result.Value;
    }

    public virtual Task DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken)
        => _queueClient.DeleteMessageAsync(messageId, popReceipt, cancellationToken);

    public virtual Task MoveToPoisonAsync(string message, CancellationToken cancellationToken)
        => _poisonQueueClient.SendMessageAsync(message, cancellationToken: cancellationToken);
}
