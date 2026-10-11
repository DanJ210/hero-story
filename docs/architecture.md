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

The original story setup is stored independently of the continuity rollup and scene state, so compaction does not remove it from later narrative prompts. Opening, continuation, revision, and conclusion use the same prompt assembly: title, genre, hero archetype and name, original setup, accepted active-path context, and the user action are JSON-encoded as story data, not instructions. The provider system prompt directs genre-neutral storytelling, honors the supplied genre and character concept without imposing unsupported fantasy or superhero tropes, and treats authored setup, contributions, and generated continuity as data rather than authority over safety or the output contract. The latest accepted state and events take precedence over obsolete initial details and older summaries; superseded events are excluded. Opening moderation checks the combined title, genre, archetype, name, and setup under a separate 5,804-code-unit limit, preserving the existing keyword, prompt-injection, and provider-category checks; ordinary user contributions retain their 2,000-character limit.

### Asynchronous path (worker-facing)

- API enqueues image work only when the validated story beat qualifies for artwork under the selective image policy.
- Worker polls queue in batches.
- Each message resolves to a `GenerationJob`; failed jobs remain eligible for bounded queue redelivery, while completed jobs are idempotently skipped. Development queue visibility is configured above observed provider latency to prevent duplicate in-flight image generation.
- Worker updates status transitions (`Pending -> Processing -> Completed/Failed/Poisoned`).
- Output assets are persisted to blob storage and referenced by domain records.
- Jobs associated only with superseded turns must not replace artwork on the active story path.

Before contacting the image provider, the worker skips jobs for superseded scenes. For eligible scenes, artwork prompt context comes from the session's genre, hero name, archetype, and original setup, plus the accepted scene's location, summary, narrative, state, and conflict. The worker normalizes whitespace, neutralizes angle-bracket data delimiters, and bounds fields (genre 500, archetype 1,000, name 100, setup 4,000, location 300, summary/narrative/state 2,000 each, and conflict 500 characters); long scene values are excerpted at a word boundary with a surrogate-safe ellipsis. The prompt treats story values as untrusted data, depicts one concrete visible accepted moment, lets accepted scene details override obsolete setup facts, and excludes raw `ChoiceText`, suggested actions, and the generation job's stored prompt. Fixed depiction and, when applicable, reference-image instructions are appended intact.

`OpenAiClient` enforces a shared 16,000-character prompt limit for ordinary `/v1/images/generations` and portrait-reference `/v1/images/edits` requests. Blank or oversized prompts are rejected before transport or reference-stream work; the client does not slice the assembled prompt. The portrait path sends a JSON image-edit request with a normalized image data URL. After provider generation, the worker rechecks that the scene remains active before publishing artwork.

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
