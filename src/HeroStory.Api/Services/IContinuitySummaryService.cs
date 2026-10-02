using HeroStory.Core.Entities;

namespace HeroStory.Api.Services;

public interface IContinuitySummaryService
{
    int RecentTurns { get; }
    int CompactionInterval { get; }
    int MaximumContextCharacters { get; }

    Task<string> CompactAsync(
        string existingSummary,
        IReadOnlyList<Scene> scenes,
        CancellationToken cancellationToken);
}