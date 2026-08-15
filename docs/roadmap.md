# Roadmap

This roadmap reflects expected progression from current MVP scaffold to production-ready application.

## Near-term (MVP completion)

1. Harden the implemented structured turn foundation:
   - add malformed-provider-response retry policy and observability,
   - add configurable retry/failure behavior for narrative or state validation failures,
   - update session status when an episode completes.
2. Add persistent turn lineage and an EF migration:
   - active/superseded status,
   - parent and revised-from relationships,
   - active-path uniqueness and concurrency protection.
3. Expand the implemented latest-turn continuity prompting:
   - compact relevant summaries across older turns,
   - measure and cap the complete prompt budget,
   - add continuity regression evaluations for facts, relationships, and unresolved threads.
4. Expand the scene API for active-path reads and latest-turn revision while preserving ownership, moderation, and normalized error contracts.
5. Apply selective artwork policy so only opening, major, climax, conclusion, or explicitly requested beats enqueue image jobs.
6. Replace the scene-card workflow with the reader-first conversational experience:
   - continuous book-like passages,
   - free-text “What does your hero do?” input,
   - optional suggested actions,
   - latest-turn revision,
   - pause, resume, and explicit episode conclusion,
   - loading, error, retry, and superseded-artwork states.
7. Add vertical-slice tests proving continuity, meaningful consequences, revision behavior, active-path ownership, episode completion, and selective image dispatch.

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
