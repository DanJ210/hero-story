using System.Collections.Concurrent;
using HeroStory.Core.Abstractions;

namespace HeroStory.Infrastructure.Services;

public sealed class InMemoryImageJobQueue : IImageJobQueue
{
    private static readonly ConcurrentQueue<Guid> Queue = new();

    public Task QueueAsync(Guid storyId, CancellationToken cancellationToken)
    {
        Queue.Enqueue(storyId);
        return Task.CompletedTask;
    }

    public Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
        => Task.FromResult(Queue.TryDequeue(out var storyId) ? (Guid?)storyId : null);
}
