using HeroStory.Core.Enums;

namespace HeroStory.Core.Entities;

public class Scene
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int SequenceNumber { get; set; }
    public Guid? ParentSceneId { get; set; }
    public Guid? RevisedFromSceneId { get; set; }
    public bool IsActive { get; set; } = true;
    public string ChoiceText { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;
    public string SceneSummary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ActiveConflict { get; set; } = string.Empty;
    public int StoryStateSchemaVersion { get; set; } = 1;
    public string StoryStateJson { get; set; } = "{}";
    public string SuggestedActionsJson { get; set; } = "[]";
    public StoryBeat StoryBeat { get; set; } = StoryBeat.Standard;
    public bool IsEpisodeComplete { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ImageUrlExpiresAt { get; set; }
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public string? ModerationDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StorySession Session { get; set; } = null!;
    public ICollection<GenerationJob> GenerationJobs { get; set; } = [];
}
