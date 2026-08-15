using HeroStory.Core.Enums;

namespace HeroStory.Api.DTOs.Scene;

public sealed record GeneratedStoryTurn(
    string NarrativeText,
    string SceneSummary,
    string Location,
    string ActiveConflict,
    string StoryStateJson,
    IReadOnlyList<string> SuggestedActions,
    StoryBeat StoryBeat,
    bool IsEpisodeComplete);