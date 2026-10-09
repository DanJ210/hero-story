# Data model

Persistence is centered on `AppDbContext` in `src/HeroStory.Infrastructure/Data/AppDbContext.cs`.

This document describes the persisted model in present tense. It does not track delivery status; see [roadmap.md](roadmap.md).

## Core entities

## `ApplicationUser`

- Extends Identity user model with application-specific fields.
- Soft-delete behavior is enforced via query filter (`IsDeleted`).

## `RefreshToken`

- Stores refresh token state for token rotation/revocation.
- Linked to user identity.

## `StorySession`

- User-owned story container.
- Query-filtered for logical deletion (`DeletedAt == null`).
- Parent for scenes and generation context.
- Stores user-authored setup text with SQL Server length limits shared through `StorySessionFieldLimits`: `Title` `nvarchar(200)`, `Genre` `nvarchar(500)`, `HeroArchetype` `nvarchar(1000)`, and `HeroName` `nvarchar(100)`. The API validates these limits before persistence, and the combined opening moderation input stays within the moderation input limit.
- Tracks the active episode through `Status` (`active`, `paused`, `completed`, `archived`, `pendingDeletion`) and carries the default-off `LikenessEnabled` opt-in.
- Persists compacted continuity in `ContinuitySummary`, the last covered scene sequence in `ContinuitySummaryThroughSequence`, and its timestamp in `ContinuitySummaryUpdatedAt`.

## `Scene`

- Represents one interactive story turn: a user contribution followed by generated narrative.
- Stores `ChoiceText`, `NarrativeText`, sequence, moderation, image metadata, scene summary, location, active conflict, schema-versioned state JSON, suggested actions JSON, story-beat classification, and episode-completion status.
- Revision lineage is immutable: a replacement turn records its parent turn and the turn it revised from, superseded versions are retained off the active path, and a unique constraint keeps one active scene per sequence number.
- A concurrency token rejects concurrent latest-turn replacements with an optimistic-concurrency conflict.

## `GenerationJob`

- Tracks async image-generation requests.
- Includes status, attempt count, and error details.
- A scene may own multiple jobs over time, so each manual or retried artwork request is preserved as history.

## `DeletionAuditLog`

- Captures account/session deletion audit metadata.

## `UserPortrait` and consent state

Hero-likeness personalization keeps portrait bytes and source URLs out of `StorySession` and `Scene` entirely. The model separates:

- `UserPortrait` — a private, user-owned record holding the blob reference, content metadata, and `DisabledAt`/`DeletedAt` retention state. Uploading a replacement disables prior versions rather than mutating them, so version history is preserved.
- `PortraitConsentRecord` — an immutable grant tied to one user and portrait version, with purpose, policy version, provider scope, and grant time. Revocation is recorded as an append-only `PortraitAuditEvent`; a grant is never edited or deleted.
- `PortraitAuditEvent` — an append-only event with actor type, subject user, event type, and only the applicable portrait, consent, session, scene, and job identifiers. It excludes image bytes, blob URLs, prompts, and secrets.
- `StorySession.LikenessEnabled` — a default-off, session-level opt-in.
- `GenerationJob.PortraitId` and `GenerationJob.PortraitConsentRecordId` — opaque portrait-version and consent provenance that lets the worker revalidate consent without exposing the blob location.

Versioned grants authorize story-artwork likeness use for the recorded provider scope. Replacement, disablement, and deletion revoke the affected grants through audit events and settle outstanding likeness jobs. Legacy timestamp-only portraits do not authorize likeness use and require a new grant.

Portrait deletion and account deletion must account for source blobs across every portrait version, derivative references, queued work, provider retention, and backup expiry.

## Supporting enums

- `SessionStatus` (`active`, `paused`, `completed`, `archived`, `pendingDeletion`)
- `JobStatus`
- `ModerationStatus`

## Relationship summary (conceptual)

1. `ApplicationUser` 1-to-many `StorySession`
2. `StorySession` 1-to-many `Scene`
3. `Scene` 1-to-many `GenerationJob` (implementation may reference by scene/job keys depending on service workflow)
4. `ApplicationUser` 1-to-many `RefreshToken`
5. `ApplicationUser` 1-to-many `UserPortrait` (one active version at a time)

Revision lineage uses self-referencing scene relationships so a replacement turn points to the preceding accepted turn and the version it supersedes. Session reads return the active path by default, and revision history is a separate representation.

## Structured story state

Validated structured state is persisted rather than inferred from prose. The contract covers:

- scene summary,
- current location and active conflict,
- known characters and relationship changes,
- established facts and constraints,
- resources or meaningful conditions,
- unresolved story threads,
- suggested actions,
- story-beat importance,
- episode-completion flag.

Use a schema-versioned structured representation. Storage may begin as provider-supported JSON for iteration, but ownership, validation, size limits, and migration strategy must remain explicit.

## Migrations and configuration

- Entity configuration classes live in the infrastructure assembly and are applied from there.
- The API applies migrations at startup when `DB_APPLY_MIGRATIONS=true`.
- Committed migrations under `src/HeroStory.Infrastructure/Data/Migrations`, in order: `InitialCreate`, `AddStructuredStoryTurn`, `AddSceneRevisionLineage`, `AddSceneConcurrencyToken`, `AllowMultipleGenerationJobsPerScene`, `AddUserPortraitConsent`, `AddPortraitProvenanceToGenerationJobs`, `AddAutomaticLikenessOptIn`, `AddContinuitySummary`, `AddPortraitConsentAudit`, `ExpandStorySessionSetupFields`.

## Related docs

- API endpoints using these entities: [api-summary.md](api-summary.md)
- Runtime architecture and async pipeline: [architecture.md](architecture.md)
- Product and revision contract: [story-experience.md](story-experience.md)
- Delivery status: [roadmap.md](roadmap.md)
