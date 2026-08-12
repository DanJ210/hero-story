using HeroStory.Core.Abstractions;
using HeroStory.Core.Domain.Entities;

namespace HeroStory.Infrastructure.Services;

public sealed class PlaceholderImageGenerator : IPlaceholderImageGenerator
{
    public Task<string> GenerateCoverAsync(HeroStoryAggregate story, CancellationToken cancellationToken)
        => Task.FromResult($"https://placehold.co/1200x800/png?text={Uri.EscapeDataString(story.HeroName)}");

    public Task<IReadOnlyList<string>> GenerateSceneImagesAsync(HeroStoryAggregate story, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(story.Scenes
            .OrderBy(scene => scene.Sequence)
            .Select(scene => $"https://placehold.co/1024x768/png?text={Uri.EscapeDataString(scene.Title)}")
            .ToArray());
}
