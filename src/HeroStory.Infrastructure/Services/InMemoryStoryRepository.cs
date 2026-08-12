using System.Collections.Concurrent;
using HeroStory.Core.Abstractions;
using HeroStory.Core.Domain.Entities;

namespace HeroStory.Infrastructure.Services;

public sealed class InMemoryStoryRepository : IStoryRepository
{
    private static readonly ConcurrentDictionary<Guid, HeroStoryAggregate> Store = new();

    public Task<HeroStoryAggregate?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Store.TryGetValue(id, out var story);
        return Task.FromResult(story is null ? null : Clone(story));
    }

    public Task<IReadOnlyCollection<HeroStoryAggregate>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<HeroStoryAggregate>>(Store.Values
            .OrderByDescending(story => story.CreatedUtc)
            .Select(Clone)
            .ToArray());

    public Task AddAsync(HeroStoryAggregate story, CancellationToken cancellationToken)
    {
        Store[story.Id] = Clone(story);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(HeroStoryAggregate story, CancellationToken cancellationToken)
    {
        Store[story.Id] = Clone(story);
        return Task.CompletedTask;
    }

    private static HeroStoryAggregate Clone(HeroStoryAggregate story)
    {
        var clone = new HeroStoryAggregate
        {
            Id = story.Id,
            HeroName = story.HeroName,
            Setting = story.Setting,
            Tone = story.Tone,
            Prompt = story.Prompt,
            Status = story.Status,
            CoverImageUrl = story.CoverImageUrl,
            FailureReason = story.FailureReason,
            CreatedUtc = story.CreatedUtc,
            UpdatedUtc = story.UpdatedUtc
        };

        clone.Scenes.AddRange(story.Scenes.Select(scene => new StoryScene
        {
            Sequence = scene.Sequence,
            Title = scene.Title,
            Narrative = scene.Narrative,
            ImagePrompt = scene.ImagePrompt,
            ImageUrl = scene.ImageUrl
        }));

        return clone;
    }
}
