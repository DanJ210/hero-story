using HeroStory.Api.DTOs.Session;
using HeroStory.Core.Entities;

namespace HeroStory.Api.Services;

/// <summary>
/// Validates user-authored story setup fields before persistence or generation.
/// Accepted text is stored exactly as submitted; nothing is trimmed or truncated.
/// </summary>
public static class StorySessionInputValidator
{
    public static void ValidateCreate(CreateSessionRequest request)
    {
        var errors = new List<string>();
        ValidateRequired(request.Title, "Title", StorySessionFieldLimits.TitleMaxLength, errors);
        ValidateRequired(request.Genre, "Genre", StorySessionFieldLimits.GenreMaxLength, errors);
        ValidateRequired(request.HeroArchetype, "Hero archetype", StorySessionFieldLimits.HeroArchetypeMaxLength, errors);
        ValidateRequired(request.HeroName, "Hero name", StorySessionFieldLimits.HeroNameMaxLength, errors);
        ThrowIfInvalid(errors);
    }

    public static void ValidatePatch(PatchSessionRequest request)
    {
        var errors = new List<string>();
        ValidateOptional(request.Title, "Title", StorySessionFieldLimits.TitleMaxLength, errors);
        ValidateOptional(request.Genre, "Genre", StorySessionFieldLimits.GenreMaxLength, errors);
        ValidateOptional(request.HeroArchetype, "Hero archetype", StorySessionFieldLimits.HeroArchetypeMaxLength, errors);
        ValidateOptional(request.HeroName, "Hero name", StorySessionFieldLimits.HeroNameMaxLength, errors);
        ThrowIfInvalid(errors);
    }

    private static void ValidateRequired(string? value, string fieldName, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
            return;
        }

        ValidateLength(value, fieldName, maxLength, errors);
    }

    // Null leaves the stored value unchanged; a provided value must be non-blank and within the limit.
    private static void ValidateOptional(string? value, string fieldName, int maxLength, List<string> errors)
    {
        if (value is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} cannot be blank.");
            return;
        }

        ValidateLength(value, fieldName, maxLength, errors);
    }

    private static void ValidateLength(string value, string fieldName, int maxLength, List<string> errors)
    {
        if (value.Length > maxLength)
        {
            errors.Add($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private static void ThrowIfInvalid(List<string> errors)
    {
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }
}
