using HeroStory.Core.Contracts;

namespace HeroStory.Core.Abstractions;

public interface IStoryService
{
    Task<StoryDto> CreateAsync(CreateStoryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<StoryDto>> ListAsync(CancellationToken cancellationToken);
    Task<StoryDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ProcessImagesAsync(Guid storyId, CancellationToken cancellationToken);
}
