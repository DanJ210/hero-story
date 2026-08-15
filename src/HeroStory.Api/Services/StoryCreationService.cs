using HeroStory.Api.DTOs.Session;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class StoryCreationService : IStoryCreationService
{
    private readonly IStoryService _storyService;
    private readonly ISceneService _sceneService;
    private readonly AppDbContext _dbContext;

    public StoryCreationService(IStoryService storyService, ISceneService sceneService, AppDbContext dbContext)
    {
        _storyService = storyService;
        _sceneService = sceneService;
        _dbContext = dbContext;
    }

    public async Task<CreateStorySessionResponse> CreateAsync(Guid userId, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _storyService.CreateSessionAsync(userId, request, cancellationToken);
        try
        {
            var openingScene = await _sceneService.CreateOpeningSceneAsync(userId, session.Id, cancellationToken);
            return new CreateStorySessionResponse(session, openingScene);
        }
        catch
        {
            await RemoveIncompleteSessionAsync(userId, session.Id, CancellationToken.None);
            throw;
        }
    }

    private async Task RemoveIncompleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(story => story.Id == sessionId && story.UserId == userId, cancellationToken);
        if (session is null)
        {
            return;
        }

        _dbContext.StorySessions.Remove(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}