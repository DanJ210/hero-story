# Architecture

This document describes how the system is structured and how a request flows through it. It states behavior in present tense and does not track delivery status; see [roadmap.md](roadmap.md) for what is complete or outstanding.

## High-level component model

1. **Frontend (Vue 3)** calls API endpoints using bearer auth.
2. **API (.NET 10)** handles auth, session, and scene orchestration.
3. **SQL Server (EF Core)** stores users, sessions, scenes, refresh tokens, and generation jobs.
4. **Azure Queue Storage** buffers image generation work.
5. **Worker (.NET 10 background service)** dequeues jobs and runs image strategies.
6. **Azure Blob Storage** stores generated image outputs and static media assets.
7. **OpenAI services** are used for text generation/moderation and optional image strategy integration.

## Request and processing boundaries

### Synchronous path (user-facing)

- Frontend sends authenticated requests to API.
- API validates identity and ownership, moderates the user's contribution, and loads the active story path and compact continuity state.
- The story service sends hero configuration, relevant summaries, current state, and the new user action to the text-generation boundary.
- Generated output is parsed and validated as structured data containing book-like narrative, a scene summary, state changes, 2–3 suggested actions, story-beat classification, and episode-completion status.
- API moderates generated prose, persists the new immutable turn on the active path, and returns it without waiting for artwork.
- Revision creates a replacement turn from the preceding accepted turn and marks the prior latest version as superseded; it does not overwrite historical content in place.

Continuation and revision insert the submitted `ChoiceText` unchanged into the prompt under "New user action". Suggested-action labels use the same contribution path. The generation prompt requests 250-500 words, and response validation enforces that word range. The product interaction contract is defined in [story-experience.md](story-experience.md#player-contribution-contract); alignment work is tracked in [roadmap.md](roadmap.md#character-roleplay-alignment).

Structured generation results are validated and persisted alongside `ChoiceText` and `NarrativeText`. Each request carries the persisted continuity rollup, the latest accepted scene's summary, location, conflict, schema-versioned state, and narrative passage, plus uncovered active-path scene summaries and state markers that fit the configured context budget. The API trims the oldest uncovered scene context first; it preserves the rollup and latest scene, so those fixed blocks can make the total context exceed the configured budget. After the accepted scene is persisted, the API uses `OpenAiClient` to compact at most one configured-size batch of the oldest eligible active-path scenes per request, then advances the rollup's covered sequence to that batch's last scene. A failed compaction leaves the accepted turn intact and preserves the prior rollup, so the same bounded batch can be retried later. Revising a scene covered by the rollup clears it so the summary can be rebuilt from the active path. Compaction output remains internal generation context and is excluded from session and workspace responses. Image jobs are created automatically for opening, major, climax, and conclusion beats, and on reader request for any active scene, and derived artwork status is exposed to clients.

### Asynchronous path (worker-facing)

- API enqueues image work only when the validated story beat qualifies for artwork under the selective image policy.
- Worker polls queue in batches.
- Each message resolves to a `GenerationJob`; failed jobs remain eligible for bounded queue redelivery, while completed jobs are idempotently skipped. Development queue visibility is configured above observed provider latency to prevent duplicate in-flight image generation.
- Worker updates status transitions (`Pending -> Processing -> Completed/Failed/Poisoned`).
- Output assets are persisted to blob storage and referenced by domain records.
- Jobs associated only with superseded turns must not replace artwork on the active story path.

## Story-state boundary

Continuity is an application-owned contract, not an unbounded chat transcript. The persisted state should contain compact facts needed to continue the story, including characters, relationships, location, active conflict, resources, unresolved threads, and summaries of prior turns.

Generation requests should use the minimum relevant context. Structured model responses must be schema-validated; do not parse narrative prose to recover state.

## Likeness boundary

Optional hero-likeness personalization is a separate privacy boundary from narrative state and generated artwork. Source portraits belong in private, ownership-scoped storage and must not be embedded in `StorySession`, `Scene`, queue payloads, logs, or public asset containers.

Consent, private portrait metadata, replacement, disablement, and deletion are owned by the portrait service. A versioned consent grant identifies its portrait version, purpose, policy version, and provider scope. API-created artwork jobs carry opaque portrait and consent-record IDs; the worker checks ownership, scope, revocation, active portrait version, and bounded provider-reference age before dispatching the source image. It records a minimal audit event at the provider boundary, normalizes source encoding, and sends the image directly to the provider's manual image-edit endpoint. It rechecks consent after the provider call and discards the result if consent or portrait state changed while the call was in flight. A provider call already in flight cannot be recalled. A default-off, session-level opt-in extends the same opaque provenance boundary to automatic artwork on opening, major, climax, and conclusion beats. Disabling or deleting a portrait revokes its consent, settles outstanding likeness jobs, and removes superseded portrait blobs, under a retain-output policy for artwork already generated.

## Security and control surfaces

- JWT bearer authentication with refresh-token workflow.
- Rate limiting policies for auth/session/scene endpoints.
- CORS policy driven by `CORS_ALLOWED_ORIGINS`.
- Middleware for correlation ID and exception handling.
- Content moderation service invoked before creating unsafe content.
- Revision and continuation endpoints enforce the same user ownership checks as reads and creation.
- Likeness upload, use, replacement, and deletion require explicit consent and ownership checks independent of story ownership.

## Deployment shape (target)

The architecture is designed for cloud deployment where API and worker are independently scalable compute units over shared SQL/queue/blob backends. The repository itself is configured for local-first development.

## Related docs

- Doc index: [application-overview.md](application-overview.md)
- API details: [api-summary.md](api-summary.md)
- Data design: [data-model.md](data-model.md)
- Setup and dev workflow: [development-guide.md](development-guide.md)
- Delivery status: [roadmap.md](roadmap.md)
- Frozen kickoff baseline: [handoff-plan.md](handoff-plan.md)
- Product and turn contract: [story-experience.md](story-experience.md)
