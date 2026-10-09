namespace HeroStory.Core.Entities;

/// <summary>
/// Maximum stored lengths for user-authored story setup fields. Shared by the EF mapping and API validation.
/// The combined maximum (plus separators) must stay within the opening-turn moderation input limit.
/// </summary>
public static class StorySessionFieldLimits
{
    public const int TitleMaxLength = 200;
    public const int GenreMaxLength = 500;
    public const int HeroArchetypeMaxLength = 1_000;
    public const int HeroNameMaxLength = 100;
}
