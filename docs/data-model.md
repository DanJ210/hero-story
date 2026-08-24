# Data model

Persistence is centered on `AppDbContext` in `src/HeroStory.Infrastructure/Data/AppDbContext.cs`.

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
- Target behavior tracks the active episode and active story path while preserving superseded revisions.

## `Scene`

- Represents one interactive story turn: a user contribution followed by generated narrative.
- The current entity stores `ChoiceText`, `NarrativeText`, sequence, moderation, image metadata, scene summary, location, active conflict, schema-versioned state JSON, suggested actions JSON, story-beat classification, and episode-completion status.
- Revision requires immutable lineage fields such as parent turn, revised-from turn, and active/superseded status. The active sequence must remain unambiguous even when historical versions are retained.

## `GenerationJob`

- Tracks async image-generation requests.
- Includes status, attempt count, and error details.

## `DeletionAuditLog`

- Captures account/session deletion audit metadata.

## `UserPortrait` and consent state

Hero-likeness personalization is implemented. Portrait bytes and source URLs must never be added to `StorySession` or `Scene`.

The current model separates:

- `UserPortrait` — a private, user-owned record holding the blob reference, content metadata, `ConsentGrantedAt`, and `DisabledAt`/`DeletedAt` retention state. Uploading a replacement disables prior versions rather than mutating them, so version history is preserved.
- `StorySession.LikenessEnabled` — a default-off, session-level opt-in.
- `GenerationJob.PortraitId` and `GenerationJob.PortraitConsentGrantedAt` — opaque generated-asset provenance that lets the worker revalidate consent without exposing the blob location.

Consent is currently a timestamp on the portrait record rather than a separate immutable consent entity. A dedicated consent record covering purpose, policy version, and provider scope, plus an audit trail for upload, use, replacement, disablement, export, and deletion, is still outstanding.

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

Target revision lineage adds self-referencing scene relationships so a replacement turn can point to the preceding accepted turn and the version it supersedes. Session reads return the active path by default; revision history is a separate representation.

## Target structured story state

The application should persist validated structured state rather than infer continuity from prose. The initial contract includes:

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

## Notes on current scaffold vs target

- Entity configuration classes are present and applied from infrastructure assembly.
- The initial EF Core migration is committed under `src/HeroStory.Infrastructure/Data/Migrations`.
- The API applies migrations at startup when `DB_APPLY_MIGRATIONS=true`; deployment-safe rollout automation remains a production-readiness task.
- The structured turn fields are introduced by the `AddStructuredStoryTurn` migration. The model does not yet implement revision lineage, active-path semantics, or session-level episode transitions.

## Related docs

- API endpoints using these entities: [api-summary.md](api-summary.md)
- Runtime architecture and async pipeline: [architecture.md](architecture.md)
- Product and revision contract: [story-experience.md](story-experience.md)
