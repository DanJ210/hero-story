using HeroStory.Api.DTOs.Session;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class StoryService : IStoryService
{
    private readonly AppDbContext _dbContext;

    public StoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
            .Select(x => new SessionDto(x.Id, x.Title, x.Genre, x.HeroArchetype, x.HeroName, x.Status, x.ModerationFailureCount, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SessionDto> CreateSessionAsync(Guid userId, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var session = new StorySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Genre = request.Genre,
            HeroArchetype = request.HeroArchetype,
            HeroName = request.HeroName,
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.StorySessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SessionDto(session.Id, session.Title, session.Genre, session.HeroArchetype, session.HeroName, session.Status, session.ModerationFailureCount, session.CreatedAt, session.UpdatedAt);
    }

    public async Task<SessionDto?> PatchSessionAsync(Guid userId, Guid sessionId, PatchSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.StorySessions.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return null;
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
        return new SessionDto(session.Id, session.Title, session.Genre, session.HeroArchetype, session.HeroName, session.Status, session.ModerationFailureCount, session.CreatedAt, session.UpdatedAt);
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
}
