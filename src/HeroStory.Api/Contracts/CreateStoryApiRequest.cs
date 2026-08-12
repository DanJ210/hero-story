using System.ComponentModel.DataAnnotations;

namespace HeroStory.Api.Contracts;

public sealed class CreateStoryApiRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string HeroName { get; init; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Setting { get; init; } = string.Empty;

    [StringLength(40)]
    public string Tone { get; init; } = "hopeful";

    [Required]
    [StringLength(500, MinimumLength = 8)]
    public string Prompt { get; init; } = string.Empty;
}
