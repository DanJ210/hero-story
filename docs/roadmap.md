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
- Development authentication, SQL migrations, OpenAI moderation, and safe external-service errors support local vertical-slice testing.

### Current milestone

Add immutable latest-turn revision and active-path semantics, then expose revision from the reader-first workspace. The workspace is now usable for opening, reading, resuming, and continuing a linear story; revision requires persistence guarantees before a UI affordance is added.

### Deferred until the workspace is usable

- explicit episode conclusion and session-status transitions,
- multi-turn summary compaction,
- explicit artwork requests and retries,
- hero-likeness personalization.

## Near-term (MVP completion)

1. Add persistent turn lineage and an EF migration:
   - active/superseded status,
   - parent and revised-from relationships,
   - active-path uniqueness and concurrency protection.
2. Expand workspace and scene APIs for active-path reads and latest-turn revision while preserving ownership, moderation, and normalized error contracts.
3. Add workspace revision UX:
   - revise only the latest active user action in the MVP,
   - warn that continuing moves the active path,
   - preserve the superseded turn and stop its artwork from replacing active-path media,
   - return focus and scroll position to the replacement turn.
4. Add revision vertical-slice tests for active-path order, ownership, concurrent successors, superseded artwork, and workspace rendering.
5. Harden the implemented structured turn foundation:
   - add malformed-provider-response retry policy and observability,
   - add configurable retry/failure behavior for narrative or state validation failures,
   - update session status when an episode completes.
6. Expand the implemented latest-turn continuity prompting:
   - compact relevant summaries across older turns,
   - measure and cap the complete prompt budget,
   - add continuity regression evaluations for facts, relationships, and unresolved threads.
7. Add episode pause/conclusion commands and update session status from explicit user intent and validated completion metadata.
8. Extend the implemented selective artwork policy with explicit user requests, retry commands, and idempotent dispatch.
9. Add vertical-slice tests proving episode completion, multi-turn continuity, and explicit artwork retry.

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
