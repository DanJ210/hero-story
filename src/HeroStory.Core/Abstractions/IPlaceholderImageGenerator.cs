using HeroStory.Core.Domain.Entities;

namespace HeroStory.Core.Abstractions;

public interface IPlaceholderImageGenerator
{
    Task<string> GenerateCoverAsync(HeroStoryAggregate story, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GenerateSceneImagesAsync(HeroStoryAggregate story, CancellationToken cancellationToken);
}
