using HeroStory.Core.Enums;

namespace HeroStory.Core.Entities;

public class GenerationJob
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public Guid SessionId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public int AttemptCount { get; set; } = 0;
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Scene Scene { get; set; } = null!;
}
