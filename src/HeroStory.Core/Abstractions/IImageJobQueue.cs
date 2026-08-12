namespace HeroStory.Core.Abstractions;

public interface IImageJobQueue
{
    Task QueueAsync(Guid storyId, CancellationToken cancellationToken);
    Task<Guid?> DequeueAsync(CancellationToken cancellationToken);
}
