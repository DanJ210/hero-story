namespace HeroStory.Core.Entities;

public class DeletionAuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int SessionsRemoved { get; set; }
    public int ScenesRemoved { get; set; }
    public int BlobsRemoved { get; set; }
    public string HashedIp { get; set; } = string.Empty;
}
