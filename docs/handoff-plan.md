# Interactive Hero Story MVP — GitHub Copilot Agent Handoff Package

**Version:** 1.1  
**Date:** Aug 12, 2026  
**Status:** Frozen baseline. Amend only when the product contract itself changes, and bump the version when you do.

This page records the baseline agreed at project kickoff. It is a historical reference for original scope and intent, not a description of current behavior. Diff proposed work against it to detect scope drift.

## Baseline statement

The handoff package defines the authoritative MVP scope, target architecture, service boundaries, and delivery expectations for Hero Story. Current code should align to this baseline while allowing iterative implementation details.

## Baseline MVP scope

In scope:

- authentication and account lifecycle,
- user-owned story sessions,
- scene creation through moderated text generation,
- asynchronous image generation through a queue and worker.

Deferred beyond the MVP:

- collaboration and shared authoring,
- deep personalization,
- non-critical platform integrations.

## Baseline architecture commitments

- API, worker, frontend, SQL persistence, queue orchestration, and blob storage are the primary components.
- Queue decoupling isolates user-facing latency from image generation workloads.
- Core entities cover user, story session, scene, generation job, and token lifecycle, with explicit ownership boundaries and state transitions for asynchronous processing.
- Authenticated REST endpoints use normalized DTO contracts and robust error handling.
- Text generation is paired with moderation in the request path, and image generation sits behind a strategy abstraction so placeholder and provider-backed implementations are interchangeable.
- JWT auth, rate limiting, secure middleware defaults, retry, poison-queue handling, and audit-friendly failure handling are required, not optional.
- Development is local-first with containerized dependencies, environment-variable configuration, and test-first quality gates.
- Vertical slices come before optimization, and documentation stays in parity with implemented capabilities.

## Baseline acceptance framing

The MVP is accepted when baseline flows operate across frontend, API, queue, worker, and storage boundaries with observable state transitions.

## Where current direction lives

This page does not track progress or restate the current product contract. Each of those has exactly one home:

- Product, turn, revision, artwork, and likeness contract: [story-experience.md](story-experience.md). Where this baseline is broad, that document controls the current interpretation.
- Delivery status, sequencing, and deferrals: [roadmap.md](roadmap.md).
- Implemented behavior: [architecture.md](architecture.md), [api-summary.md](api-summary.md), and [data-model.md](data-model.md).

## Related docs

- Doc index: [application-overview.md](application-overview.md)
- Architecture realization: [architecture.md](architecture.md)
- API implementation summary: [api-summary.md](api-summary.md)
- Data model realization: [data-model.md](data-model.md)
