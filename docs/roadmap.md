# Roadmap

This roadmap reflects expected progression from current MVP scaffold to production-ready application.

## Delivery status

### Implemented foundation

- Session creation generates and returns a persisted opening turn.
- Turn generation returns validated structured narrative, continuity state, suggestions, story beat, and episode-completion metadata.
- The latest accepted turn provides bounded continuity context to the next generation request.
- Opening, major, climax, and conclusion beats selectively enqueue artwork; standard turns do not.
- Scene APIs expose artwork status, the worker stores signed image URLs, and the frontend polls only active artwork jobs.
- An owner-scoped workspace endpoint returns session metadata and ordered full turns without N+1 scene-detail requests.
- The responsive story workspace provides a desktop story rail, mobile drawer, continuous reading timeline, inline artwork states, optional suggestions, and a persistent hero-action composer.
- Immutable latest-turn revision persists parent and revised-from lineage, retains superseded turns outside the active path, and enforces one active scene per sequence number.
- Active-path scene and workspace reads exclude superseded turns; continuation records parent lineage.
- The workspace exposes an inline latest-turn revision editor, refreshes the active timeline after replacement, and restores focus to the replacement turn.
- Development authentication, SQL migrations, OpenAI moderation, and safe external-service errors support local vertical-slice testing.

### Current milestone

Begin consent-driven hero-likeness personalization. The first phase isolates portrait storage and consent from story generation; no portrait is used by the worker or provider until the privacy foundation is complete.

### Completed Milestones

- [x] Persist revision lineage and active-path uniqueness with an EF Core migration.
- [x] Add owner-scoped active-path reads and latest-turn revision API behavior.
- [x] Add latest-turn workspace revision with replacement focus and timeline refresh.
- [x] Add focused unit and frontend-store coverage for revision behavior.
- [x] Add HTTP active-path filtering and revision ownership coverage.
- [x] Reject concurrent latest-turn replacements with an optimistic-concurrency conflict.
- [x] Prevent late artwork jobs from attaching media to superseded scenes.
- [x] Add pause, resume, and explicit conclusion commands with owner-scoped status transitions.
- [x] Complete a session when a validated turn confirms episode completion and keep its active path readable.
- [x] Make the workspace status-aware by disabling continuation for paused or completed episodes.
- [x] Allow users to request artwork manually for any active-path scene and request a new image after the prior job settles.
- [x] Retry malformed structured-turn responses with bounded configuration and validation-attempt observability.
- [x] Include bounded older active-path continuity summaries and state markers in generation prompts.
- [x] Retry failed artwork jobs through bounded queue redelivery without regenerating completed jobs.

### Hero-likeness phases

- [x] Phase 1: private portrait metadata, consent, ownership-scoped upload/delete, and retention state.
- [x] Phase 2: portrait-version provenance and short-lived provider-reference issuance for manual artwork only.
   - [x] Store opaque portrait ID and consent timestamp on opted-in manual artwork jobs.
   - [x] Reject likeness requests without an active consented portrait.
   - [x] Add an explicit per-scene likeness opt-in control, defaulting off.
   - [x] Resolve private portraits in the worker and send them directly as multipart input for manual image edits.
   - [x] Add provider-reference expiry/provenance policy and complete the manual likeness evaluation coverage.
- [ ] Phase 3: optional automatic likeness artwork, disable/replace flows, deletion semantics, and end-to-end policy tests.
   - [x] Slice 1: persist a default-off session likeness opt-in and attach active portrait provenance to automatic opening, major, climax, and conclusion artwork jobs.
   - [x] Slice 2: add portrait disable/replace flows and enforce active consented portrait provenance so stale queued likeness jobs fail closed.
   - [ ] Slice 3: complete deletion semantics so portrait and account deletion remove superseded portrait blobs, settle outstanding likeness jobs, and apply a stated retention policy for artwork already generated from the deleted source.
   - [ ] Slice 4: add end-to-end likeness policy tests covering missing consent, reference expiry, superseded scenes, and deletion during an in-flight job.

### Still deferred

- multi-turn summary compaction.

## Near-term (MVP completion)

1. Add vertical-slice tests proving automatic artwork retry and completed-job idempotency.

## Mid-term (production readiness)

1. Add robust observability:
   - structured logging,
   - distributed tracing,
   - metrics and dashboards.
2. Add migration/versioning strategy and deployment-safe schema rollout process.
3. Improve worker resilience:
   - idempotency protection,
   - backoff strategies,
   - dead-letter replay workflows.
4. Add stronger security controls:
   - secret rotation and managed identity integration,
   - stricter CSP/CORS policy management,
   - audit and compliance reporting.
5. Resolve product policy for age bands, content ratings, romance, irreversible outcomes, story sharing, and retention.

## Longer-term (product capabilities)

1. Expand the story model beyond MVP:
   - revision of older turns,
   - multiple active branches and branch comparison,
   - multi-episode hero campaigns,
   - collaborative sessions.
2. Support multi-strategy image generation policy by tier, cost, or quality.
3. Add personalization features and recommendation signals.
   - Add opt-in hero-likeness portraits only after the reader/chat and selective-artwork flows are stable.
   - Implement consent records, ownership-scoped private portrait storage, portrait versioning, short-lived provider access, provenance, deletion/export, retention, and provider-policy enforcement.
   - Keep likeness analysis out of scope; use portraits only as an authorized generation reference.
4. Expand platform integrations for analytics and content safety governance.

## Baseline reference

The direction above is anchored in the official handoff baseline documented in [handoff-plan.md](handoff-plan.md).
