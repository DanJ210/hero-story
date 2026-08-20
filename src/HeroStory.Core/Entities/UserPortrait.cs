namespace HeroStory.Core.Entities;

public class UserPortrait
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public DateTime ConsentGrantedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
