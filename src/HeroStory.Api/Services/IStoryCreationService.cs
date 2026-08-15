using HeroStory.Api.DTOs.Session;

namespace HeroStory.Api.Services;

public interface IStoryCreationService
{
    Task<CreateStorySessionResponse> CreateAsync(Guid userId, CreateSessionRequest request, CancellationToken cancellationToken);
}