using HeroStory.Core.Enums;
using HeroStory.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace HeroStory.Api.Services;

public class ModerationService : IModerationService
{
    private static readonly string[] InjectionIndicators = ["ignore previous", "system prompt", "developer message", "bypass"];
    private readonly OpenAiClient _openAiClient;
    private readonly string[] _keywords;

    public ModerationService(OpenAiClient openAiClient, IConfiguration configuration)
    {
        _openAiClient = openAiClient;
        var keywordPath = configuration["MODERATION_KEYWORD_LIST_PATH"];
        _keywords = !string.IsNullOrWhiteSpace(keywordPath) && File.Exists(keywordPath)
            ? File.ReadAllLines(keywordPath).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray()
            : [];
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

        return await _openAiClient.IsFlaggedAsync(input, cancellationToken)
            ? (ModerationStatus.Rejected, "Input was flagged by OpenAI moderation.")
            : (ModerationStatus.Approved, null);
    }

    public async Task<(ModerationStatus Status, string? Detail, string Narrative)> ModerateOutputAsync(string output, CancellationToken cancellationToken)
    {
        if (await _openAiClient.IsFlaggedAsync(output, cancellationToken))
        {
            return (ModerationStatus.Sanitized, "Output was flagged by OpenAI moderation.", "The hero pauses, choosing a safer path forward.");
        }

        return (ModerationStatus.Approved, null, output);
    }
}
