using HeroStory.Core.Enums;

namespace HeroStory.Core.Entities;

public class StorySession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string HeroArchetype { get; set; } = string.Empty;
    public string HeroName { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public int ModerationFailureCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Scene> Scenes { get; set; } = [];
}
