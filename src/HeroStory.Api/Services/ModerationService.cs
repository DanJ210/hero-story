using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Api.Services;

public class ModerationService : IModerationService
{
    private static readonly string[] InjectionIndicators = ["ignore previous", "system prompt", "developer message", "bypass"];

    // Superhero/sci-fi action is the product genre, so "violence" and non-threatening "harassment" are not blocking.
    private static readonly string[] DefaultBlockedCategories =
    [
        "sexual/minors",
        "hate",
        "hate/threatening",
        "harassment/threatening",
        "self-harm",
        "self-harm/intent",
        "self-harm/instructions",
        "illicit",
        "illicit/violent",
        "violence/graphic",
        OpenAiClient.UnspecifiedModerationCategory
    ];

    private readonly OpenAiClient _openAiClient;
    private readonly string[] _keywords;
    private readonly HashSet<string> _blockedCategories;

    public ModerationService(OpenAiClient openAiClient, IConfiguration configuration)
    {
        _openAiClient = openAiClient;
        var keywordPath = configuration["MODERATION_KEYWORD_LIST_PATH"];
        _keywords = !string.IsNullOrWhiteSpace(keywordPath) && File.Exists(keywordPath)
            ? File.ReadAllLines(keywordPath).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray()
            : [];
        var configuredCategories = configuration["MODERATION_BLOCKED_CATEGORIES"];
        _blockedCategories = new HashSet<string>(
            string.IsNullOrWhiteSpace(configuredCategories)
                ? DefaultBlockedCategories
                : configuredCategories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<(ModerationStatus Status, string? Detail)> ModerateInputAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 2_000)
        {
            return (ModerationStatus.Rejected, "Input length is invalid.");
        }

        if (_keywords.Any(keyword => input.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return (ModerationStatus.Rejected, "Input matched a blocked keyword.");
        }

        if (InjectionIndicators.Any(indicator => input.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            return (ModerationStatus.Rejected, "Input resembled a prompt injection attempt.");
        }

        var blockedInputCategories = await GetBlockingCategoriesAsync(input, cancellationToken);
        return blockedInputCategories.Length > 0
            ? (ModerationStatus.Rejected, $"Input was flagged by OpenAI moderation ({string.Join(", ", blockedInputCategories)}).")
            : (ModerationStatus.Approved, null);
    }

    public async Task<(ModerationStatus Status, string? Detail, string Narrative)> ModerateOutputAsync(string output, CancellationToken cancellationToken)
    {
        var blockedOutputCategories = await GetBlockingCategoriesAsync(output, cancellationToken);
        if (blockedOutputCategories.Length > 0)
        {
            return (
                ModerationStatus.Sanitized,
                $"Output was flagged by OpenAI moderation ({string.Join(", ", blockedOutputCategories)}).",
                "The hero pauses, choosing a safer path forward.");
        }

        return (ModerationStatus.Approved, null, output);
    }

    private async Task<string[]> GetBlockingCategoriesAsync(string text, CancellationToken cancellationToken)
    {
        var flaggedCategories = await _openAiClient.GetFlaggedCategoriesAsync(text, cancellationToken);
        return flaggedCategories.Where(_blockedCategories.Contains).ToArray();
    }
}
