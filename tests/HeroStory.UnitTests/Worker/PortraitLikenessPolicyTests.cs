using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Worker;
using Microsoft.Extensions.Configuration;

namespace HeroStory.UnitTests.Worker;

public class PortraitLikenessPolicyTests
{
    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentRecordMissingOnJob()
    {
        var now = DateTime.UtcNow;
        var portraitId = Guid.NewGuid();
        var portrait = CreatePortrait(now.AddMinutes(-1), portraitId);
        var job = CreateJob(now, consentRecordId: null, portraitId);

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, null, portrait.UserId, false, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitConsentMissing, exception.Code);
        Assert.Contains("missing a consent record", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentRecordReferencesAnotherPortrait()
    {
        var now = DateTime.UtcNow;
        var portraitId = Guid.NewGuid();
        var portrait = CreatePortrait(now.AddMinutes(-10), portraitId);
        var consentRecord = CreateConsentRecord(now.AddMinutes(-20), Guid.NewGuid(), portrait.UserId);
        var job = CreateJob(now, consentRecord.Id, portraitId);

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, consentRecord, portrait.UserId, false, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitProvenanceMismatch, exception.Code);
        Assert.Contains("no longer matches", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentIsRevoked()
    {
        var now = DateTime.UtcNow;
        var portrait = CreatePortrait(now.AddMinutes(-10), Guid.NewGuid());
        var consentRecord = CreateConsentRecord(now.AddMinutes(-10), portrait.Id, portrait.UserId);
        var job = CreateJob(now, consentRecord.Id, portrait.Id);

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, consentRecord, portrait.UserId, true, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitConsentMissing, exception.Code);
        Assert.Contains("revoked", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentScopeIsInvalid()
    {
        var now = DateTime.UtcNow;
        var portrait = CreatePortrait(now.AddMinutes(-10), Guid.NewGuid());
        var consentRecord = CreateConsentRecord(now.AddMinutes(-10), portrait.Id, portrait.UserId);
        consentRecord = new PortraitConsentRecord
        {
            Id = consentRecord.Id,
            UserId = consentRecord.UserId,
            PortraitId = consentRecord.PortraitId,
            Purpose = consentRecord.Purpose,
            PolicyVersion = consentRecord.PolicyVersion,
            ProviderScope = "other-provider",
            GrantedAt = consentRecord.GrantedAt
        };
        var job = CreateJob(now, consentRecord.Id, portrait.Id);

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, consentRecord, portrait.UserId, false, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitConsentMissing, exception.Code);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenReferenceIsExpired()
    {
        var now = DateTime.UtcNow;
        var portraitId = Guid.NewGuid();
        var consentGrantedAt = now.AddMinutes(-180);
        var portrait = CreatePortrait(consentGrantedAt, portraitId);
        var consentRecord = CreateConsentRecord(consentGrantedAt, portraitId, portrait.UserId);
        var job = CreateJob(now.AddMinutes(-180), consentRecord.Id, portraitId);

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, consentRecord, portrait.UserId, false, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitReferenceExpired, exception.Code);
        Assert.Contains("reference expired", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenJobReferencesSupersededPortrait()
    {
        var now = DateTime.UtcNow;
        var consentGrantedAt = now.AddMinutes(-10);
        var activePortrait = CreatePortrait(consentGrantedAt, Guid.NewGuid());
        var consentRecord = CreateConsentRecord(consentGrantedAt, activePortrait.Id, activePortrait.UserId);
        var job = CreateJob(now, consentRecord.Id, Guid.NewGuid());

        var exception = Assert.Throws<ArtworkPolicyException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, activePortrait, consentRecord, activePortrait.UserId, false, now, TimeSpan.FromMinutes(120)));

        Assert.Equal(ArtworkErrorCode.PortraitProvenanceMismatch, exception.Code);
        Assert.Contains("active portrait", exception.Message);
    }

    [Fact]
    public void ResolveReferenceMaxAge_UsesDefaultWhenConfigIsMissingOrInvalid()
    {
        var missing = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var invalid = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LIKENESS_PROVIDER_REFERENCE_MAX_AGE_MINUTES"] = "0"
        }).Build();

        var missingAge = PortraitLikenessPolicy.ResolveReferenceMaxAge(missing);
        var invalidAge = PortraitLikenessPolicy.ResolveReferenceMaxAge(invalid);

        Assert.Equal(TimeSpan.FromMinutes(120), missingAge);
        Assert.Equal(TimeSpan.FromMinutes(120), invalidAge);
    }

    private static GenerationJob CreateJob(DateTime createdAt, Guid? consentRecordId, Guid portraitId)
        => new()
        {
            Id = Guid.NewGuid(),
            SceneId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PortraitId = portraitId,
            PortraitConsentRecordId = consentRecordId,
            Prompt = "Prompt",
            Status = JobStatus.Queued,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static UserPortrait CreatePortrait(DateTime consentGrantedAt, Guid portraitId)
        => new()
        {
            Id = portraitId,
            UserId = Guid.NewGuid(),
            BlobName = "users/test/portrait",
            ContentType = "image/jpeg",
            ContentLength = 1024,
            CreatedAt = consentGrantedAt
        };

    private static PortraitConsentRecord CreateConsentRecord(DateTime grantedAt, Guid portraitId, Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PortraitId = portraitId,
            Purpose = PortraitConsentPolicy.Purpose,
            PolicyVersion = PortraitConsentPolicy.PolicyVersion,
            ProviderScope = PortraitConsentPolicy.ProviderScope,
            GrantedAt = grantedAt
        };
}