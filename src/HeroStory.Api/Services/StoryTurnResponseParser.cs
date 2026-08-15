using System.Text.Json;
using System.Text;
using HeroStory.Api.DTOs.Scene;
using HeroStory.Core.Enums;

namespace HeroStory.Api.Services;

public static class StoryTurnResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static GeneratedStoryTurn Parse(string response)
    {
        StoryTurnResponse parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<StoryTurnResponse>(response, SerializerOptions)
                ?? throw new InvalidOperationException("Story turn response was empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Story turn response was not valid JSON.", exception);
        }

        RequireValue(parsed.Narrative, "narrative");
        RequireValue(parsed.SceneSummary, "sceneSummary");
        RequireValue(parsed.Location, "location");
        RequireValue(parsed.ActiveConflict, "activeConflict");
        ValidateLength(parsed.SceneSummary, "sceneSummary", StoryTurnLimits.MaximumSceneSummaryCharacters);
        ValidateLength(parsed.Location, "location", StoryTurnLimits.MaximumLocationCharacters);
        ValidateLength(parsed.ActiveConflict, "activeConflict", StoryTurnLimits.MaximumActiveConflictCharacters);

        var narrative = parsed.Narrative.Trim();
        var narrativeWordCount = narrative.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (narrativeWordCount is < StoryTurnLimits.MinimumNarrativeWords or > StoryTurnLimits.MaximumNarrativeWords)
        {
            throw new InvalidOperationException($"Story turn narrative must contain {StoryTurnLimits.MinimumNarrativeWords}-{StoryTurnLimits.MaximumNarrativeWords} words.");
        }

        var suggestions = parsed.SuggestedActions?
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Select(action => action.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (suggestions.Length is < 2 or > 3)
        {
            throw new InvalidOperationException("Story turn response must include 2 or 3 suggested actions.");
        }
        if (suggestions.Any(action => action.Length > StoryTurnLimits.MaximumSuggestedActionCharacters))
        {
            throw new InvalidOperationException($"Each suggested action must be at most {StoryTurnLimits.MaximumSuggestedActionCharacters} characters.");
        }

        if (!Enum.TryParse<StoryBeat>(parsed.StoryBeat, true, out var storyBeat))
        {
            throw new InvalidOperationException("Story turn response included an invalid storyBeat.");
        }

        if (parsed.StoryState.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Story turn response must include storyState as an object.");
        }

        var storyStateJson = JsonSerializer.Serialize(parsed.StoryState);
        if (Encoding.UTF8.GetByteCount(storyStateJson) > StoryTurnLimits.MaximumStoryStateBytes)
        {
            throw new InvalidOperationException($"Story turn storyState must be at most {StoryTurnLimits.MaximumStoryStateBytes} bytes.");
        }

        return new GeneratedStoryTurn(
            narrative,
            parsed.SceneSummary.Trim(),
            parsed.Location.Trim(),
            parsed.ActiveConflict.Trim(),
            storyStateJson,
            suggestions,
            storyBeat,
            parsed.IsEpisodeComplete);
    }

    private static void RequireValue(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Story turn response must include {propertyName}.");
        }
    }

    private static void ValidateLength(string value, string propertyName, int maximumCharacters)
    {
        if (value.Trim().Length > maximumCharacters)
        {
            throw new InvalidOperationException($"Story turn {propertyName} must be at most {maximumCharacters} characters.");
        }
    }

    private sealed record StoryTurnResponse(
        string Narrative,
        string SceneSummary,
        string Location,
        string ActiveConflict,
        JsonElement StoryState,
        IReadOnlyList<string> SuggestedActions,
        string StoryBeat,
        bool IsEpisodeComplete);
}