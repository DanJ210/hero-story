using HeroStory.Core.Entities;
using HeroStory.Core.Enums;

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
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitConsentMissing, "Likeness provenance is missing consent timestamp.");
        }

        if (portrait.ConsentGrantedAt != job.PortraitConsentGrantedAt.Value)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitProvenanceMismatch, "Portrait consent provenance no longer matches the active portrait.");
        }

        if (nowUtc - job.CreatedAt > maxAge)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitReferenceExpired, "The portrait likeness reference expired before generation started. Request artwork again.");
        }
    }
}