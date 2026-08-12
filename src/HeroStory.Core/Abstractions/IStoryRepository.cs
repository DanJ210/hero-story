using HeroStory.Core.Domain.Entities;

namespace HeroStory.Core.Abstractions;

public interface IStoryRepository
{
    Task<HeroStoryAggregate?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<HeroStoryAggregate>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(HeroStoryAggregate story, CancellationToken cancellationToken);
    Task UpdateAsync(HeroStoryAggregate story, CancellationToken cancellationToken);
}
