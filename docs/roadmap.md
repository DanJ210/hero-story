# Roadmap

This roadmap reflects expected progression from current MVP scaffold to production-ready application.

## Near-term (MVP completion)

1. Finalize API contract consistency and error envelope standards.
2. Expand scene and generation job endpoints with richer status polling/query.
3. Add committed local infrastructure orchestration (`docker-compose.yml`) for one-command startup.
4. Harden frontend UX for loading/error/retry states across auth/session/scene flows.

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

## Longer-term (product capabilities)

1. Introduce richer story model:
   - branching storylines,
   - collaborative sessions,
   - revision history.
2. Support multi-strategy image generation policy by tier, cost, or quality.
3. Add personalization features and recommendation signals.
4. Expand platform integrations for analytics and content safety governance.

## Baseline reference

The direction above is anchored in the official handoff baseline documented in [handoff-plan.md](handoff-plan.md).
