# Interactive Hero Story MVP — GitHub Copilot Agent Handoff Package

**Version:** 1.0  
**Date:** Aug 12, 2026  
**Status:** Baseline specification for this repository

This page summarizes the official handoff package that kicked off implementation. It should be treated as the baseline product and architecture specification against which scaffold progress is tracked.

## Baseline statement

The handoff package defines the authoritative MVP scope, target architecture, service boundaries, and delivery expectations for Hero Story. Current code should align to this baseline while allowing iterative implementation details.

## Product-direction addendum

The interactive experience has been refined without changing the baseline architecture:

- The user is the superhero and primary protagonist.
- The story advances one conversational turn at a time rather than generating a full novel.
- Each turn produces approximately 250–500 words of book-like prose.
- Users can provide free-text actions or select from 2–3 optional suggestions.
- User decisions must create observable consequences and update explicit continuity state.
- Users can revise the latest active turn; revisions preserve prior versions and move the active story path to the replacement.
- Artwork is asynchronous and selective, reserved for opening scenes and major story beats.
- Episodes have explicit completion state and can be paused or resumed.
- Optional use of the user's own likeness in hero artwork is a post-chat-flow capability and must be consent-driven, privacy-isolated, and removable.

The detailed product, turn, revision, and acceptance contracts are maintained in [story-experience.md](story-experience.md). Where the original baseline is broad, that document controls the current product interpretation. Current code must not be described as implementing target behavior until the corresponding roadmap work is complete.

## Section-by-section summary

## 1. Product vision and goals

- Deliver an interactive hero-story experience combining guided narrative generation with image support.
- Emphasize safe content generation, authenticated user ownership, and extensible architecture.

## 2. MVP scope definition

- Include core flows for auth, session creation, scene creation, and asynchronous image generation.
- Defer advanced collaboration, deep personalization, and non-critical platform integrations to later phases.

## 3. System architecture blueprint

- Define API, worker, frontend, SQL persistence, queue orchestration, and blob storage as primary components.
- Use queue decoupling to isolate user-facing latency from image generation workloads.

## 4. Data model and domain contracts

- Establish core entities for user, story session, scene, generation job, and token lifecycle.
- Require clear ownership boundaries and state transitions for asynchronous processing.

## 5. API and integration expectations

- Provide authenticated REST endpoints for core user interactions.
- Normalize DTO-based contracts and support robust error handling.

## 6. AI and moderation strategy

- Use OpenAI text generation with moderation checks in the request path.
- Support image generation strategy abstraction to enable placeholder and provider-backed implementations.

## 7. Security and reliability guardrails

- Require JWT auth, rate limiting, and baseline secure middleware behavior.
- Include retry, poison queue, and audit-friendly failure handling for async jobs.

## 8. Developer workflow and environment

- Define local-first setup, environment variable configuration, and test-first quality gates.
- Expect containerized local dependencies and reproducible service startup workflow.

## 9. Delivery sequencing

- Prioritize scaffolding service boundaries and end-to-end vertical slices before optimization.
- Maintain documentation parity with implemented capabilities.

## 10. Acceptance framing

- MVP is accepted when baseline flows operate across frontend, API, queue, worker, and storage boundaries with observable state transitions.

## How to use this baseline now

1. Use this handoff as the source of truth for intended architecture.
2. Document current scaffold behavior without overstating completion.
3. Track divergences or deferrals explicitly in [roadmap.md](roadmap.md).

## Related docs

- Overview: [application-overview.md](application-overview.md)
- Architecture realization: [architecture.md](architecture.md)
- API implementation summary: [api-summary.md](api-summary.md)
- Data model realization: [data-model.md](data-model.md)
