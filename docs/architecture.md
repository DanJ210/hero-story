# Architecture

This document describes the intended target architecture and how the current scaffold maps to it.

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

The current implementation validates and persists the structured generation result alongside `ChoiceText` and `NarrativeText`. It supplies the latest accepted summary, location, conflict, schema-versioned state, and narrative passage to the next request. It creates image jobs automatically for opening, major, climax, and conclusion beats, and on reader request for any active scene, while exposing derived artwork status to clients. Revision history, multi-turn summary compaction, and automatic image retry remain planned work.

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

## Future likeness boundary

Optional hero-likeness personalization is a separate privacy boundary from narrative state and generated artwork. Source portraits belong in private, ownership-scoped storage and must not be embedded in `StorySession`, `Scene`, queue payloads, logs, or public asset containers.

An eventual likeness service should own consent, portrait versions, retention/deletion status, and generation-reference issuance. Image jobs should carry an opaque portrait-version identifier; workers resolve it to short-lived authorized provider access only when policy permits. Generated assets record provenance without exposing the source portrait.

## Security and control surfaces

- JWT bearer authentication with refresh-token workflow.
- Rate limiting policies for auth/session/scene endpoints.
- CORS policy driven by `CORS_ALLOWED_ORIGINS`.
- Middleware for correlation ID and exception handling.
- Content moderation service invoked before creating unsafe content.
- Revision and continuation endpoints enforce the same user ownership checks as reads and creation.
- Future likeness upload, use, replacement, and deletion require explicit consent and ownership checks independent of story ownership.

## Deployment shape (target)

The architecture is designed for cloud deployment where API and worker are independently scalable compute units over shared SQL/queue/blob backends. The current repository scaffold focuses on local-first development and service boundaries needed for that deployment model.

## Related docs

- Overview: [application-overview.md](application-overview.md)
- API details: [api-summary.md](api-summary.md)
- Data design: [data-model.md](data-model.md)
- Setup and dev workflow: [development-guide.md](development-guide.md)
- Baseline handoff spec: [handoff-plan.md](handoff-plan.md)
- Product and turn contract: [story-experience.md](story-experience.md)
