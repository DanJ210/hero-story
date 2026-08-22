using HeroStory.Core.Entities;

namespace HeroStory.Worker;

internal static class PortraitLikenessPolicy
{
    private const int DefaultReferenceMaxAgeMinutes = 120;

    public static TimeSpan ResolveReferenceMaxAge(IConfiguration configuration)
    {
        var configuredMinutes = int.TryParse(configuration["LIKENESS_PROVIDER_REFERENCE_MAX_AGE_MINUTES"], out var parsedMinutes)
            ? parsedMinutes
            : DefaultReferenceMaxAgeMinutes;
        var minutes = configuredMinutes > 0 ? configuredMinutes : DefaultReferenceMaxAgeMinutes;
        return TimeSpan.FromMinutes(minutes);
    }

    public static void ValidateForGeneration(GenerationJob job, UserPortrait portrait, DateTime nowUtc, TimeSpan maxAge)
    {
        if (job.PortraitConsentGrantedAt is null)
        {
            throw new InvalidOperationException("Likeness provenance is missing consent timestamp.");
        }

        if (portrait.ConsentGrantedAt != job.PortraitConsentGrantedAt.Value)
        {
            throw new InvalidOperationException("Portrait consent provenance no longer matches the active portrait.");
        }

        if (nowUtc - job.CreatedAt > maxAge)
        {
            throw new InvalidOperationException("The portrait likeness reference expired before generation started. Request artwork again.");
        }
    }
}