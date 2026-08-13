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

## `Scene`

- Child content element within a story session.
- Stores prompt/narrative output and related metadata.

## `GenerationJob`

- Tracks async image-generation requests.
- Includes status, attempt count, and error details.

## `DeletionAuditLog`

- Captures account/session deletion audit metadata.

## Supporting enums

- `SessionStatus`
- `JobStatus`
- `ModerationStatus`

## Relationship summary (conceptual)

1. `ApplicationUser` 1-to-many `StorySession`
2. `StorySession` 1-to-many `Scene`
3. `Scene` 1-to-many `GenerationJob` (implementation may reference by scene/job keys depending on service workflow)
4. `ApplicationUser` 1-to-many `RefreshToken`

## Notes on current scaffold vs target

- Entity configuration classes are present and applied from infrastructure assembly.
- Schema evolution/migrations are expected to mature with deployment automation.
- Current model supports MVP flows while leaving room for richer story metadata and asset history.

## Related docs

- API endpoints using these entities: [api-summary.md](api-summary.md)
- Runtime architecture and async pipeline: [architecture.md](architecture.md)
