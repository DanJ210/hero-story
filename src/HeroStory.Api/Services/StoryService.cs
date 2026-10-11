using HeroStory.Api.DTOs.Session;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class StoryService : IStoryService
{
    private readonly AppDbContext _dbContext;
    private readonly IModerationService _moderationService;

    public StoryService(AppDbContext dbContext, IModerationService moderationService)
    {
        _dbContext = dbContext;
        _moderationService = moderationService;
    }

    public async Task<IReadOnlyList<SessionListDto>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => await _dbContext.StorySessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new SessionListDto(x.Id, x.Title, x.Genre, x.HeroName, x.Status, x.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
        => await _dbContext.StorySessions
            .Where(x => x.UserId == userId && x.Id == sessionId)
            .Select(x => new SessionDto(x.Id, x.Title, x.Genre, x.HeroArchetype, x.HeroName, x.Status, x.ModerationFailureCount, x.CreatedAt, x.UpdatedAt, x.LikenessEnabled, x.StorySetup))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<StoryWorkspaceDto?> GetWorkspaceAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(story => story.Scenes)
                .ThenInclude(scene => scene.GenerationJobs)
            .SingleOrDefaultAsync(story => story.Id == sessionId && story.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var sessionDto = ToDto(session);
        var turns = session.Scenes.Where(scene => scene.IsActive).OrderBy(scene => scene.SequenceNumber).Select(SceneDtoMapper.ToDto).ToArray();
        return new StoryWorkspaceDto(sessionDto, turns);
    }

    public async Task<SessionDto> CreateSessionAsync(Guid userId, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        StorySessionInputValidator.ValidateCreate(request);
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Genre = request.Genre,
            HeroArchetype = request.HeroArchetype,
            HeroName = request.HeroName,
            StorySetup = request.StorySetup,
            LikenessEnabled = request.LikenessEnabled,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.StorySessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<SessionDto?> PatchSessionAsync(Guid userId, Guid sessionId, PatchSessionRequest request, CancellationToken cancellationToken)
    {
        StorySessionInputValidator.ValidatePatch(request);
        var session = await _dbContext.StorySessions.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (request.StorySetup is not null && request.StorySetup != session.StorySetup)
        {
            if (await _dbContext.Scenes.AnyAsync(scene => scene.SessionId == sessionId, cancellationToken))
            {
                throw new InvalidOperationException("Story setup cannot be changed after the episode begins.");
            }

            if (!string.IsNullOrWhiteSpace(request.StorySetup))
            {
                var moderation = await _moderationService.ModerateSetupAsync(request.StorySetup, cancellationToken);
                if (moderation.Status != ModerationStatus.Approved)
                {
                    throw new InvalidOperationException(moderation.Detail ?? "Story setup was rejected by moderation.");
                }
            }
            session.StorySetup = request.StorySetup;
        }

        session.Title = request.Title ?? session.Title;
        session.Genre = request.Genre ?? session.Genre;
        session.HeroArchetype = request.HeroArchetype ?? session.HeroArchetype;
        session.HeroName = request.HeroName ?? session.HeroName;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<SessionStatus>(request.Status, true, out var status))
        {
            session.Status = status;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<SessionDto?> TransitionStatusAsync(Guid userId, Guid sessionId, SessionStatus status, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (status == SessionStatus.Paused && session.Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Only an active episode can be paused.");
        }

        if (status == SessionStatus.Active && session.Status != SessionStatus.Paused)
        {
            throw new InvalidOperationException("Only a paused episode can be resumed.");
        }

        session.Status = status;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(session);
    }

    public async Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        session.DeletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SessionDto ToDto(StorySession session)
        => new(session.Id, session.Title, session.Genre, session.HeroArchetype, session.HeroName, session.Status, session.ModerationFailureCount, session.CreatedAt, session.UpdatedAt, session.LikenessEnabled, session.StorySetup);
}
