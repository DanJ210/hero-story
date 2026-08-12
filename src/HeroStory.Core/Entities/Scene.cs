using HeroStory.Core.Enums;

namespace HeroStory.Core.Entities;

public class Scene
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int SequenceNumber { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public string NarrativeText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime? ImageUrlExpiresAt { get; set; }
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public string? ModerationDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StorySession Session { get; set; } = null!;
    public GenerationJob? GenerationJob { get; set; }
}
