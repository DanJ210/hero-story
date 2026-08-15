namespace HeroStory.Api.Services;

public static class StoryTurnLimits
{
    public const int MinimumNarrativeWords = 250;
    public const int MaximumNarrativeWords = 500;
    public const int MaximumSceneSummaryCharacters = 2_000;
    public const int MaximumLocationCharacters = 300;
    public const int MaximumActiveConflictCharacters = 1_000;
    public const int MaximumStoryStateBytes = 16_384;
    public const int MaximumSuggestedActionCharacters = 300;
}