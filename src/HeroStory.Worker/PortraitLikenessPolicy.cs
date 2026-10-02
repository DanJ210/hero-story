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

    public static void ValidateForGeneration(
        GenerationJob job,
        UserPortrait portrait,
        PortraitConsentRecord? consentRecord,
        Guid expectedUserId,
        bool consentRevoked,
        DateTime nowUtc,
        TimeSpan maxAge)
    {
        if (job.PortraitId != portrait.Id)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitProvenanceMismatch, "Portrait provenance no longer matches the active portrait.");
        }

        if (job.PortraitConsentRecordId is null || consentRecord is null)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitConsentMissing, "Likeness provenance is missing a consent record.");
        }

        if (consentRecord.Id != job.PortraitConsentRecordId.Value
            || consentRecord.PortraitId != portrait.Id
            || consentRecord.UserId != expectedUserId)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitProvenanceMismatch, "Portrait consent provenance no longer matches the active portrait.");
        }

        if (consentRevoked
            || consentRecord.Purpose != PortraitConsentPolicy.Purpose
            || consentRecord.PolicyVersion != PortraitConsentPolicy.PolicyVersion
            || consentRecord.ProviderScope != PortraitConsentPolicy.ProviderScope)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitConsentMissing, "Portrait consent is invalid or has been revoked.");
        }

        if (nowUtc - job.CreatedAt > maxAge)
        {
            throw new ArtworkPolicyException(ArtworkErrorCode.PortraitReferenceExpired, "The portrait likeness reference expired before generation started. Request artwork again.");
        }
    }
}