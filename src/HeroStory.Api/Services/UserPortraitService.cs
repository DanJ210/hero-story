using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Data;
using HeroStory.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace HeroStory.Api.Services;

public class UserPortraitService : IUserPortraitService
{
    private const long MaximumPortraitBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly AppDbContext _dbContext;
    private readonly AzureBlobService _blobService;
    private readonly IConfiguration _configuration;

    public UserPortraitService(AppDbContext dbContext, AzureBlobService blobService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _blobService = blobService;
        _configuration = configuration;
    }

    public async Task<PortraitDto> UploadAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength,
        bool consentGranted,
        string consentPolicyVersion,
        CancellationToken cancellationToken)
    {
        if (!consentGranted)
        {
            throw new InvalidOperationException("Explicit portrait consent is required.");
        }
        if (!string.Equals(consentPolicyVersion, PortraitConsentPolicy.PolicyVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The portrait consent notice changed. Review the current notice and consent again.");
        }
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Portrait must be a JPEG, PNG, or WebP image.");
        }
        if (contentLength <= 0 || contentLength > MaximumPortraitBytes)
        {
            throw new InvalidOperationException("Portrait must be between 1 byte and 10 MB.");
        }

        var now = DateTime.UtcNow;
        var activePortraits = await _dbContext.UserPortraits
            .Where(portrait => portrait.UserId == userId && portrait.DeletedAt == null && portrait.DisabledAt == null)
            .ToListAsync(cancellationToken);
        foreach (var activePortrait in activePortraits)
        {
            activePortrait.DisabledAt = now;
        }

        var portrait = new UserPortrait
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlobName = $"users/{userId}/portraits/{Guid.NewGuid():N}",
            ContentType = contentType,
            ContentLength = contentLength,
            CreatedAt = now
        };
        var consentRecord = new PortraitConsentRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortraitId = portrait.Id,
            Purpose = PortraitConsentPolicy.Purpose,
            PolicyVersion = PortraitConsentPolicy.PolicyVersion,
            ProviderScope = PortraitConsentPolicy.ProviderScope,
            GrantedAt = now
        };
        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        await _blobService.UploadAsync(containerName, portrait.BlobName, content, contentType, cancellationToken);
        _dbContext.UserPortraits.Add(portrait);
        _dbContext.PortraitConsentRecords.Add(consentRecord);
        _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(userId, userId, "user", "consent_granted", portrait.Id, consentRecord.Id, occurredAt: now));
        _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(userId, userId, "user", "portrait_uploaded", portrait.Id, consentRecord.Id, occurredAt: now));
        foreach (var oldPortrait in activePortraits)
        {
            _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(
                userId, userId, "user", "portrait_replaced", oldPortrait.Id, relatedPortraitId: portrait.Id, occurredAt: now));
        }
        await RevokeConsentAsync(userId, activePortraits.Select(candidate => candidate.Id).ToArray(), "portrait_replaced", userId, now, cancellationToken);
        await SettleOutstandingJobsAsync(userId, activePortraits.Select(candidate => candidate.Id).ToArray(), "PortraitReplaced", now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(portrait, consentValid: true);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var hasPortrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null)
            .AnyAsync(cancellationToken);
        if (!hasPortrait)
        {
            return false;
        }

        await PurgeAsync(userId, cancellationToken);
        return true;
    }

    public async Task<PortraitPurgeResult> PurgeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portraits = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ToListAsync(cancellationToken);

        var blobNames = portraits
            .Select(portrait => portrait.BlobName)
            .Where(blobName => !string.IsNullOrWhiteSpace(blobName))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var now = DateTime.UtcNow;
        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        foreach (var blobName in blobNames)
        {
            await _blobService.DeleteAsync(containerName, blobName, cancellationToken);
        }

        foreach (var portrait in portraits)
        {
            portrait.DisabledAt ??= now;
            portrait.DeletedAt = now;
            _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(userId, userId, "user", "portrait_deleted", portrait.Id, occurredAt: now));
        }

        var portraitIds = portraits.Select(candidate => candidate.Id).ToArray();
        await RevokeConsentAsync(userId, portraitIds, "portrait_deleted", userId, now, cancellationToken);
        var outstandingJobs = await SettleOutstandingJobsAsync(userId, portraitIds, "PortraitDeleted", now, cancellationToken);

        await DisableSessionLikenessAsync(userId, portraitIds, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PortraitPurgeResult(portraits.Count, blobNames.Count, outstandingJobs);
    }

    public async Task<bool> DisableAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activePortraits = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .ToListAsync(cancellationToken);
        if (activePortraits.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var portrait in activePortraits)
        {
            portrait.DisabledAt = now;
            _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(userId, userId, "user", "portrait_disabled", portrait.Id, occurredAt: now));
        }

        var portraitIds = activePortraits.Select(candidate => candidate.Id).ToArray();
        await RevokeConsentAsync(userId, portraitIds, "portrait_disabled", userId, now, cancellationToken);
        await SettleOutstandingJobsAsync(userId, portraitIds, "PortraitDisabled", now, cancellationToken);
        await DisableSessionLikenessAsync(userId, portraitIds, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserPortraitReference?> GetActiveReferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (portrait is null)
        {
            return null;
        }

        var consent = await GetValidConsentAsync(userId, portrait.Id, cancellationToken);
        return consent is null ? null : new UserPortraitReference(portrait.Id, consent.Id);
    }

    public async Task<PortraitDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (portrait is null)
        {
            return null;
        }

        var consent = await GetValidConsentAsync(userId, portrait.Id, cancellationToken);
        return ToDto(portrait, consent is not null);
    }

    private PortraitDto ToDto(UserPortrait portrait, bool consentValid)
        => new(portrait.Id, portrait.ContentType, portrait.ContentLength, portrait.CreatedAt, consentValid);

    public async Task<PortraitContent?> GetActiveContentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portrait = await _dbContext.UserPortraits
            .Where(candidate => candidate.UserId == userId && candidate.DeletedAt == null && candidate.DisabledAt == null)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (portrait is null)
        {
            return null;
        }

        var containerName = _configuration["AZURE_BLOB_PORTRAITS_CONTAINER"] ?? "hero-story-portraits";
        var stream = await _blobService.DownloadAsync(containerName, portrait.BlobName, cancellationToken);
        return new PortraitContent(stream, portrait.ContentType);
    }

    private Task<PortraitConsentRecord?> GetValidConsentAsync(Guid userId, Guid portraitId, CancellationToken cancellationToken)
        => _dbContext.PortraitConsentRecords
            .AsNoTracking()
            .Where(record => record.UserId == userId
                && record.PortraitId == portraitId
                && record.Purpose == PortraitConsentPolicy.Purpose
                && record.PolicyVersion == PortraitConsentPolicy.PolicyVersion
                && record.ProviderScope == PortraitConsentPolicy.ProviderScope
                && !_dbContext.PortraitAuditEvents.Any(audit => audit.ConsentRecordId == record.Id && audit.EventType == "consent_revoked"))
            .OrderByDescending(record => record.GrantedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task RevokeConsentAsync(
        Guid userId,
        IReadOnlyCollection<Guid> portraitIds,
        string reason,
        Guid actorUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (portraitIds.Count == 0)
        {
            return;
        }

        var consentRecords = await _dbContext.PortraitConsentRecords
            .Where(record => record.UserId == userId && portraitIds.Contains(record.PortraitId))
            .ToListAsync(cancellationToken);
        foreach (var record in consentRecords)
        {
            var alreadyRevoked = await _dbContext.PortraitAuditEvents
                .AnyAsync(audit => audit.ConsentRecordId == record.Id && audit.EventType == "consent_revoked", cancellationToken);
            if (!alreadyRevoked)
            {
                _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(
                    userId, actorUserId, "user", "consent_revoked", record.PortraitId, record.Id, detailCode: reason, occurredAt: now));
            }
        }
    }

    private async Task<int> SettleOutstandingJobsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> portraitIds,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (portraitIds.Count == 0)
        {
            return 0;
        }

        var sessionIds = await _dbContext.StorySessions
            .Where(session => session.UserId == userId)
            .Select(session => session.Id)
            .ToListAsync(cancellationToken);
        var jobs = await _dbContext.GenerationJobs
            .Where(job => job.PortraitId.HasValue
                && portraitIds.Contains(job.PortraitId.Value)
                && sessionIds.Contains(job.SessionId)
                && (job.Status == JobStatus.Queued || job.Status == JobStatus.Processing || job.Status == JobStatus.Failed))
            .ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            job.Status = JobStatus.Poisoned;
            job.CompletedAt ??= now;
            job.ErrorDetail = $"{reason}: Likeness consent was revoked before artwork generation completed.";
            job.UpdatedAt = now;
            _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(
                userId,
                userId,
                "user",
                "likeness_job_settled",
                job.PortraitId,
                job.PortraitConsentRecordId,
                sessionId: job.SessionId,
                sceneId: job.SceneId,
                generationJobId: job.Id,
                detailCode: reason,
                occurredAt: now));
        }

        return jobs.Count;
    }

    private static PortraitAuditEvent CreateAuditEvent(
        Guid subjectUserId,
        Guid? actorUserId,
        string actorType,
        string eventType,
        Guid? portraitId = null,
        Guid? consentRecordId = null,
        Guid? relatedPortraitId = null,
        Guid? sessionId = null,
        Guid? sceneId = null,
        Guid? generationJobId = null,
        string? detailCode = null,
        DateTime? occurredAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SubjectUserId = subjectUserId,
            ActorType = actorType,
            ActorUserId = actorUserId,
            EventType = eventType,
            PortraitId = portraitId,
            ConsentRecordId = consentRecordId,
            RelatedPortraitId = relatedPortraitId,
            SessionId = sessionId,
            SceneId = sceneId,
            GenerationJobId = generationJobId,
            DetailCode = detailCode,
            OccurredAt = occurredAt ?? DateTime.UtcNow
        };

    private async Task DisableSessionLikenessAsync(
        Guid userId,
        IReadOnlyCollection<Guid> portraitIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var optedInSessions = await _dbContext.StorySessions
            .Where(session => session.UserId == userId && session.LikenessEnabled)
            .ToListAsync(cancellationToken);
        var consent = portraitIds.Count == 0
            ? null
            : await _dbContext.PortraitConsentRecords
                .Where(record => record.UserId == userId && portraitIds.Contains(record.PortraitId))
                .OrderByDescending(record => record.GrantedAt)
                .FirstOrDefaultAsync(cancellationToken);
        foreach (var session in optedInSessions)
        {
            session.LikenessEnabled = false;
            session.UpdatedAt = now;
            _dbContext.PortraitAuditEvents.Add(CreateAuditEvent(
                userId,
                userId,
                "user",
                "session_likeness_disabled",
                consent?.PortraitId,
                consent?.Id,
                sessionId: session.Id,
                occurredAt: now));
        }
    }
}
