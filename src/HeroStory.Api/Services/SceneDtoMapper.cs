using System.Text.Json;
using HeroStory.Api.DTOs.Scene;
using HeroStory.Core.Entities;
using HeroStory.Core.Enums;

namespace HeroStory.Api.Services;

public static class SceneDtoMapper
{
    public static SceneDto ToDto(Scene scene)
        => new(
            scene.Id,
            scene.SessionId,
            scene.SequenceNumber,
            scene.ChoiceText,
            scene.NarrativeText,
            scene.SceneSummary,
            scene.Location,
            scene.ActiveConflict,
            scene.StoryStateSchemaVersion,
            DeserializeStoryState(scene.StoryStateJson),
            DeserializeSuggestedActions(scene.SuggestedActionsJson),
            scene.StoryBeat,
            scene.IsEpisodeComplete,
            GetArtworkStatus(GetLatestJob(scene)?.Status),
            GetArtworkErrorCode(GetLatestJob(scene)),
            scene.ImageUrl,
            scene.ImageUrlExpiresAt,
            scene.ModerationStatus,
            scene.ModerationDetail,
            scene.CreatedAt,
            scene.UpdatedAt);

    public static SceneListDto ToListDto(Scene scene)
        => new(
            scene.Id,
            scene.SequenceNumber,
            scene.ChoiceText,
            GetArtworkStatus(GetLatestJob(scene)?.Status),
            GetArtworkErrorCode(GetLatestJob(scene)),
            scene.ImageUrl,
            scene.ModerationStatus,
            scene.UpdatedAt);

    private static IReadOnlyList<string> DeserializeSuggestedActions(string value)
        => string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<string[]>(value) ?? [];

    private static JsonElement DeserializeStoryState(string value)
        => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(value) ? "{}" : value);

    private static GenerationJob? GetLatestJob(Scene scene)
        => scene.GenerationJobs.OrderByDescending(job => job.CreatedAt).ThenByDescending(job => job.Id).FirstOrDefault();

    private static string? GetArtworkErrorCode(GenerationJob? job)
    {
        if (job?.Status is not (JobStatus.Failed or JobStatus.Poisoned) || string.IsNullOrWhiteSpace(job.ErrorDetail))
        {
            return null;
        }

        var separatorIndex = job.ErrorDetail.IndexOf(':');
        var candidate = separatorIndex > 0 ? job.ErrorDetail[..separatorIndex] : null;
        return candidate is ArtworkErrorCode.PortraitUnavailable
            or ArtworkErrorCode.PortraitConsentMissing
            or ArtworkErrorCode.PortraitProvenanceMismatch
            or ArtworkErrorCode.PortraitReferenceExpired
            ? candidate
            : null;
    }

    private static ArtworkStatus GetArtworkStatus(JobStatus? jobStatus)
        => jobStatus switch
        {
            null => ArtworkStatus.NotRequested,
            JobStatus.Queued => ArtworkStatus.Queued,
            JobStatus.Processing => ArtworkStatus.Processing,
            JobStatus.Completed => ArtworkStatus.Completed,
            JobStatus.Failed => ArtworkStatus.Failed,
            JobStatus.Poisoned => ArtworkStatus.Poisoned,
            _ => throw new ArgumentOutOfRangeException(nameof(jobStatus), jobStatus, "Unsupported artwork job status.")
        };
}