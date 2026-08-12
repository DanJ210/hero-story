using HeroStory.Core.Abstractions;
using HeroStory.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HeroStory.Infrastructure.Services;

public sealed class FileKeywordModerator : IContentModerator
{
    private readonly string[] _keywords;

    public FileKeywordModerator(IOptions<ModerationOptions> options, ILogger<FileKeywordModerator> logger)
    {
        var path = options.Value.KeywordListPath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }

        if (File.Exists(path))
        {
            _keywords = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }
        else
        {
            logger.LogWarning("Moderation keyword file not found at {Path}. Using empty keyword list.", path);
            _keywords = [];
        }
    }

    public bool IsAllowed(string text, out string? reason)
    {
        foreach (var keyword in _keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Prompt contains blocked keyword '{keyword}'.";
                return false;
            }
        }

        reason = null;
        return true;
    }
}
