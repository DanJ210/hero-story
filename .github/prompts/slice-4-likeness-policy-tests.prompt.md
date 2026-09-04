---
mode: agent
agent: hero-story-slice-implementer
description: "Phase 3 Slice 4: end-to-end likeness policy tests for missing consent, reference expiry, superseded scenes, and deletion during an in-flight job."
---

Implement Phase 3, Slice 4 from `docs/roadmap.md`:

> Add end-to-end likeness policy tests covering missing consent, reference expiry, superseded scenes, and deletion during an in-flight job.

## Scope boundary

This is a test-coverage slice. Do not change likeness behavior. Only touch production code if a test proves a real policy gap, and if you find one, report it before fixing it rather than silently changing semantics. Do not refactor the worker, strategy, or portrait service for testability beyond the minimum seam needed; prefer fakes injected through the existing constructors.

Layers in play: tests only (`tests/HeroStory.UnitTests`, `tests/HeroStory.IntegrationTests`), plus a minimal seam in `src/HeroStory.Worker` only if unavoidable.

## Current state (verified)

Policy enforcement already exists:

- `HeroStory.Worker/PortraitLikenessPolicy.ValidateForGeneration(job, portrait, nowUtc, maxAge)` throws `ArtworkPolicyException` with codes from `HeroStory.Core.Enums.ArtworkErrorCode`: `PortraitUnavailable`, `PortraitConsentMissing`, `PortraitProvenanceMismatch`, `PortraitReferenceExpired`.
- `HeroStory.Worker/DallE3Strategy.GenerateAsync` resolves the active portrait, runs the policy, and after generation reloads the scene and calls `CompleteSupersededJobAsync` when `!scene.IsActive`.
- `HeroStory.Worker/ImageGenerationWorker.ProcessMessageAsync` maps `ArtworkPolicyException` into `job.ErrorDetail` as `"{code}: {message}"` and marks the job `Failed`, or `Poisoned` at max dequeue count.
- `HeroStory.Api/Services/UserPortraitService.PurgeAsync` deletes blobs, stamps `DisabledAt`/`DeletedAt`, and settles outstanding Queued/Processing/Failed likeness jobs to `Poisoned` with `"PortraitDeleted: ..."`.
- `HeroStory.Api/Services/AuthService.DeleteAccountAsync` calls `PurgeAsync` and writes a `DeletionAuditLog`.

Already covered by unit tests; do not duplicate:

- `tests/HeroStory.UnitTests/Worker/PortraitLikenessPolicyTests.cs` — missing consent timestamp, consent mismatch, expiry, superseded portrait, max-age config fallback.
- `tests/HeroStory.UnitTests/Services/UserPortraitServiceTests.cs` — replace/disable/delete/purge counts and job settling.
- `tests/HeroStory.UnitTests/Services/SceneServiceTests.cs` — provenance attached on manual and automatic likeness, and rejection without an active portrait.
- `tests/HeroStory.UnitTests/Services/AuthServiceTests.cs` — account deletion purges portraits.

## Gaps to close

There is no coverage of the worker execution path; `DallE3Strategy.GenerateAsync` and `ImageGenerationWorker.ProcessMessageAsync` are never exercised. Close these four:

1. Missing consent — a job whose portrait is no longer active or consented reaches the worker. Assert the job ends `Failed` (or `Poisoned` at max dequeue), `ErrorDetail` starts with the expected `ArtworkErrorCode`, no image is written to the scene, and no image-generation call reaches the OpenAI client fake.
2. Reference expiry — job `CreatedAt` older than the configured `LIKENESS_PROVIDER_REFERENCE_MAX_AGE_MINUTES`. Assert `PortraitReferenceExpired`, fail-closed behavior, and no provider call. Drive time explicitly rather than sleeping.
3. Superseded scene — the scene is marked `IsActive = false` while the job is in flight. Assert the job settles as `Completed`, the superseded scene gains no new `ImageUrl`, and the active replacement scene is untouched.
4. Deletion during an in-flight job — a Queued or Processing likeness job exists when `UserPortraitService.DeleteAsync` runs, and separately when `AuthService.DeleteAccountAsync` runs. Assert the job is `Poisoned` with the `PortraitDeleted` detail, session `LikenessEnabled` is cleared, portrait blobs are removed, and already-`Completed` artwork is retained per the stated retention policy in `docs/story-experience.md`.

## Approach constraints

- Prefer worker-level tests under `tests/HeroStory.UnitTests/Worker` using EF InMemory plus fakes for the OpenAI client, blob service, and queue. There is no existing worker integration harness; do not build a new bespoke one.
- Reuse existing fixture conventions for any integration coverage: `tests/HeroStory.IntegrationTests/ApiFixture.cs`, `DevelopmentApiFixture`, and dev-login auth as used in `tests/HeroStory.IntegrationTests/Profile/ProfileEndpointTests.cs`. When adjusting EF registrations, remove every production provider registration, including `IDbContextOptionsConfiguration<AppDbContext>`, before adding InMemory.
- Assert on `ArtworkErrorCode` constants, not hardcoded strings.
- Every negative test must prove the flow fails closed: no image persisted, no provider call, and no attachment to a superseded or deleted-source scene.
- No real OpenAI, Azure Blob, Azure Queue, or SQL Server calls.

## Validation

Run the targeted suites first, then the full solution:

```
dotnet build hero-story.sln
dotnet test hero-story.sln --no-build
```

Stop any running `HeroStory.Api` or `HeroStory.Worker` process before building to avoid `MSB3021`/`MSB3027` file-lock failures.

## Done means

All four scenarios have tests that exercise the real worker path and fail if the behavior regresses, the full solution builds and tests green, and `docs/roadmap.md` Phase 3 Slice 4 is checked off. Report any policy gap you discovered rather than papering over it in the test.
