# API summary

Base route prefix: `/api`

The API is implemented in `src/HeroStory.Api` using controller-based endpoints and DTO contracts. This document describes the endpoints that exist today; outstanding work is tracked in [roadmap.md](roadmap.md).

## Authentication endpoints (`/api/auth`)

- `POST /api/auth/register`
  - Creates user account.
  - Returns `201 Created` with registration response.
- `POST /api/auth/login`
  - Authenticates user credentials.
  - Returns token payload (access + refresh).
- `POST /api/auth/refresh`
  - Exchanges refresh token for new token payload.
- `POST /api/auth/logout`
  - Revokes refresh token context.
  - Returns `204 No Content`.
- `POST /api/auth/dev-login`
  - Development-only shortcut that creates or reuses the configured development user and returns the normal token payload.
  - Mapped only when the host environment is `Development` and `DEV_AUTH_ENABLED=true`.
- `DELETE /api/auth/account`
  - Authenticated account deletion request.
  - Returns `202 Accepted`.

## Profile portrait endpoints (`/api/profile/portrait`)

All routes require authentication and are scoped to the calling user. Portrait blobs live in a private container and are never returned as public URLs.

- `GET /api/profile/portrait`
  - Returns the active portrait metadata, or `404` when none is active.
- `POST /api/profile/portrait`
  - Multipart upload of `file` plus a `consentGranted` form field; rejects uploads without consent.
  - Limited to 10 MB. Uploading a replacement disables prior versions instead of mutating them.
  - Returns `201 Created` with portrait metadata.
- `POST /api/profile/portrait/disable`
  - Disables the active portrait, clears session likeness opt-ins, and fails stale queued likeness jobs closed.
  - Returns `204 No Content`, or `404` when no active portrait exists.
- `DELETE /api/profile/portrait`
  - Deletes portrait blobs across versions and settles outstanding likeness jobs. Artwork already generated is retained as story output.
  - Returns `204 No Content`, or `404` when no active portrait exists.

## Story session endpoints (`/api/sessions`)

- `GET /api/sessions`
  - Lists current user's sessions.
- `POST /api/sessions`
  - Creates a session and immediately generates its opening story turn from the supplied title, genre, hero archetype, and hero name.
  - Accepts an optional `likenessEnabled` flag, defaulting to `false`, that opts the session into automatic likeness artwork.
  - Returns `201 Created` with `{ session, openingScene }`.
  - Removes the newly created session if opening generation fails, preventing empty stories from remaining in the session list.
- `GET /api/sessions/{id}`
  - Gets single session.
  - Returns `404` if not found/user-mismatched.
- `GET /api/sessions/{id}/workspace`
  - Returns the owned session and its ordered full turn DTOs for the reader workspace.
  - Includes artwork status and signed image URLs without requiring one detail request per turn.
  - Returns `404` if not found or not owned by the authenticated user.
- `POST /api/sessions/{id}/pause`
  - Pauses an active episode and returns the updated session.
- `POST /api/sessions/{id}/resume`
  - Resumes a paused episode and returns the updated session.
- `POST /api/sessions/{id}/conclusion`
  - Generates a final turn for an active episode.
  - Requires the structured provider result to confirm `isEpisodeComplete`; the session then transitions to `completed`.
- `PATCH /api/sessions/{id}`
  - Updates mutable session state.
- `DELETE /api/sessions/{id}`
  - Soft-deletes/marks session removal.

## Scene endpoints (`/api/sessions/{id}/scenes`)

- `GET /api/sessions/{id}/scenes`
  - Lists the active story path for the owned session in sequence order, excluding superseded revisions.
- `POST /api/sessions/{id}/scenes`
  - Continues an existing story from a user action and triggers generation workflow.
  - Returns structured narrative fields: summary, location, active conflict, schema-versioned state object, 2–3 suggested actions, story beat, and episode-completion status.
  - Returns `201 Created`.
- `GET /api/sessions/{id}/scenes/{sceneId}`
  - Retrieves active scene detail.
- `POST /api/sessions/{id}/scenes/{sceneId}/revisions`
  - Revises the latest active turn using a replacement user contribution.
  - Preserves the original turn as superseded and returns the active replacement turn.
  - Returns `404` for a non-active or unowned turn and rejects any active turn that is not the latest.

Scene detail and list responses include an `artworkStatus` value: `notRequested`, `queued`, `processing`, `completed`, `failed`, or `poisoned`, plus a nullable sanitized `artworkErrorCode` for known portrait-policy failures: `portraitUnavailable`, `portraitConsentMissing`, `portraitProvenanceMismatch`, or `portraitReferenceExpired`. Opening, major, climax, and conclusion beats request artwork; standard beats do not.

- `POST /api/sessions/{id}/scenes/{sceneId}/artwork`
  - Queues an optional artwork request for an owned active scene.
  - Accepts a `usePortrait` query flag that opts the single request into likeness generation; it requires an active consented portrait and defaults to off.
  - Allows a new request after the prior job has completed, failed, or been poisoned, preserving each job as history.
  - Rejects a duplicate request while the scene already has queued or processing artwork.

Session responses include `likenessEnabled`. Session status values are `active`, `paused`, `completed`, `archived`, and `pendingDeletion`; paused and completed episodes reject new contributions while remaining readable.

All continuation and revision operations require authentication, session ownership, input/output moderation, and optimistic conflict handling so concurrent submissions cannot create two active successors accidentally.

## Generation jobs (`/api/jobs`)

- `GET /api/jobs/{jobId}`
  - Retrieves a single generation job (status, attempts, error detail).
  - Returns `404` if not found or not owned by the authenticated user.
## Cross-cutting behavior

- JWT bearer auth is required except on allow-anonymous auth endpoints.
- Rate limiter policies are configured for register, login, sessions, and scenes flows.
- JSON contract uses camelCase.
- Exception middleware returns normalized error responses.
- Required external-service failures, including OpenAI rate-limit or quota failures, return `503 Service Unavailable` without exposing provider details.

## Related docs

- Runtime architecture: [architecture.md](architecture.md)
- Entity model backing these endpoints: [data-model.md](data-model.md)
- Product and turn contract: [story-experience.md](story-experience.md)
- Delivery status: [roadmap.md](roadmap.md)
