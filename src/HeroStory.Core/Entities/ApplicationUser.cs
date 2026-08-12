using Microsoft.AspNetCore.Identity;

namespace HeroStory.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ICollection<StorySession> Sessions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
