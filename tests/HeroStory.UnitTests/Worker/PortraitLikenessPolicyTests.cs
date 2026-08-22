using HeroStory.Core.Entities;
using HeroStory.Core.Enums;
using HeroStory.Worker;
using Microsoft.Extensions.Configuration;

namespace HeroStory.UnitTests.Worker;

public class PortraitLikenessPolicyTests
{
    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentTimestampMissingOnJob()
    {
        var now = DateTime.UtcNow;
        var job = CreateJob(now, consentGrantedAt: null);
        var portrait = CreatePortrait(now.AddMinutes(-1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, now, TimeSpan.FromMinutes(120)));

        Assert.Contains("missing consent timestamp", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenConsentTimestampNoLongerMatchesPortrait()
    {
        var now = DateTime.UtcNow;
        var job = CreateJob(now, consentGrantedAt: now.AddMinutes(-20));
        var portrait = CreatePortrait(now.AddMinutes(-10));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, now, TimeSpan.FromMinutes(120)));

        Assert.Contains("no longer matches", exception.Message);
    }

    [Fact]
    public void ValidateForGeneration_ThrowsWhenReferenceIsExpired()
    {
        var now = DateTime.UtcNow;
        var consentGrantedAt = now.AddMinutes(-180);
        var job = CreateJob(now.AddMinutes(-180), consentGrantedAt);
        var portrait = CreatePortrait(consentGrantedAt);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PortraitLikenessPolicy.ValidateForGeneration(job, portrait, now, TimeSpan.FromMinutes(120)));

        Assert.Contains("reference expired", exception.Message);
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

    private static GenerationJob CreateJob(DateTime createdAt, DateTime? consentGrantedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            SceneId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PortraitId = Guid.NewGuid(),
            PortraitConsentGrantedAt = consentGrantedAt,
            Prompt = "Prompt",
            Status = JobStatus.Queued,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static UserPortrait CreatePortrait(DateTime consentGrantedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BlobName = "users/test/portrait",
            ContentType = "image/jpeg",
            ContentLength = 1024,
            ConsentGrantedAt = consentGrantedAt,
            CreatedAt = consentGrantedAt
        };
}