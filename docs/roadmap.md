# Roadmap

This roadmap reflects expected progression from current MVP scaffold to production-ready application.

It is the single home for delivery status. Other docs describe behavior in present tense and link here instead of restating what is complete, in progress, or deferred.

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
- Versioned likeness consent, portrait-lifecycle auditing, and worker-side consent revalidation are implemented with a focused policy-review follow-up.

### Current milestone

Consent-driven hero-likeness personalization is complete. Phases 1 and 2 established private portrait storage, consent, and provenance-checked manual artwork; Phase 3 added automatic likeness artwork, disable/replace flows, deletion semantics, and end-to-end policy coverage of the worker generation path.

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
- [x] Compact older active-path scenes into a persisted multi-turn continuity summary while retaining a recent generation-context window.
- [x] Retry failed artwork jobs through bounded queue redelivery without regenerating completed jobs.

### Hero-likeness phases

- [x] Phase 1: private portrait metadata, consent, ownership-scoped upload/delete, and retention state.
- [x] Phase 2: portrait-version provenance and short-lived provider-reference issuance for manual artwork only.
   - [x] Store opaque portrait ID and consent timestamp on opted-in manual artwork jobs.
   - [x] Reject likeness requests without an active consented portrait.
   - [x] Add an explicit per-scene likeness opt-in control, defaulting off.
   - [x] Resolve private portraits in the worker and send them directly as multipart input for manual image edits.
   - [x] Add provider-reference expiry/provenance policy and complete the manual likeness evaluation coverage.
- [x] Phase 3: optional automatic likeness artwork, disable/replace flows, deletion semantics, and end-to-end policy tests.
   - [x] Slice 1: persist a default-off session likeness opt-in and attach active portrait provenance to automatic opening, major, climax, and conclusion artwork jobs.
   - [x] Slice 2: add portrait disable/replace flows and enforce active consented portrait provenance so stale queued likeness jobs fail closed.
   - [x] Slice 3: complete deletion semantics so portrait and account deletion remove superseded portrait blobs, settle outstanding likeness jobs, and apply a stated retention policy for artwork already generated from the deleted source.
   - [x] Slice 4: add end-to-end likeness policy tests covering missing consent, reference expiry, superseded scenes, and deletion during an in-flight job.
   - [x] Phase 4: replace timestamp-only consent provenance with immutable purpose/version/provider-scoped grants and append-only lifecycle audit events.
   - [x] Phase 4: enforce consent validity and revocation at the worker provider boundary, and audit requested, started, rejected, and settled likeness work.

### Still deferred

- A revision-history read endpoint and the UI that would consume it.
- Hero-likeness policy decisions remain open; suggested options and starting positions are documented in [likeness-consent-policy-review.md](likeness-consent-policy-review.md):
  1. Approve or replace the consent notice, confirm any required privacy-notice links, and decide whether the approved terms warrant a new policy version and fresh grants.
  2. Confirm the exact provider, configured image model, and reference-image operation authorized by `openai-images`; any provider, model, or purpose change needs a distinct scope and fresh consent.
  3. Decide minor eligibility and an enforceable age policy. Accounts have no age-band information, so eligibility cannot be inferred from account data.
  4. Verify provider-side portrait handling, retention, and deletion for the exact operation and account configuration; decide whether the in-flight request boundary is acceptable. App deletion cannot recall data already sent to a provider.
  5. Approve a minimized consent/audit record retention schedule and restrict access to authorized staff.
  6. Revisit export auditing when an export operation exists; there is no export endpoint or event to audit yet.

## Near-term (MVP completion)

- [x] Add vertical-slice tests proving automatic artwork retry and completed-job idempotency.

### Proposed likeness-quality workflow and documentation alignment

The consent-driven likeness path above does not establish visual-quality acceptance. This follow-up is proposed work, not shipped preview, approval, or multi-reference behavior. Start with the existing OpenAI image strategy; do not change providers or introduce per-user training before a bounded quality evaluation.

- [ ] Define the product contract in [story-experience.md](story-experience.md#hero-likeness-personalization): a consented source portrait, a superhero preview the user can approve or regenerate, a versioned costume/style specification, and scene generation anchored to the original portrait plus the approved hero reference where supported. Keep likeness optional and separate photorealism from identity consistency. Approval must not replace consent.
- [ ] Agree on a bounded evaluation: five consenting adult volunteers and six scene types per person (close-up, profile, full-body, action, low light, and multiple people). Compare the existing portrait-only path with the proposed portrait-plus-approved-hero path. Record user-rated likeness, costume consistency, scene accuracy, usable-image rate, latency, and cost per accepted image including retries. Proposed gates, subject to agreement: at least 80% accepted without regeneration and mean user-rated likeness of at least 4/5. These are targets, not measured results.
- [ ] Deliver the preview-and-approval slice before adding approved references to story artwork. Define preview visibility, replacement, disablement, deletion, retention, and consent scope before implementation. Specify which changes invalidate an approval and how queued or in-flight jobs react.
- [ ] Deliver persistent visual identity and multi-reference scene generation as a subsequent slice. Keep the original portrait as the identity anchor rather than chaining only generated scenes. Verify the exact model's reference-image support and fidelity controls without assuming every OpenAI image model accepts the same options.
- [ ] Update [architecture.md](architecture.md#likeness-boundary) after each slice to describe the verified request path and trust boundaries. Keep source portraits, identity-derived references, and any future face embeddings or adapters ownership-scoped; do not place image content or unrestricted references in queues or logs.
- [ ] Update [api-summary.md](api-summary.md) with verified preview, approval, and regeneration routes and error contracts; update [data-model.md](data-model.md) with the corresponding versioned records, provenance, relationships, and migrations. Do not document proposed fields or routes as existing behavior.
- [ ] Update [development-guide.md](development-guide.md) with validated model capabilities, configuration, and reproducible evaluation steps. Keep the README and [application-overview.md](application-overview.md) as navigation rather than duplicate workflow specifications.
- [ ] Validate the frontend-to-API-to-persistence-to-queue-to-worker flow, including consent revocation, replacement, stale approval, and superseded-scene cases. Record quality results separately from policy-test success before marking the quality milestone complete.
- [ ] Only if the hosted path misses agreed quality gates, benchmark a commercially licensed identity-conditioned alternative behind the strategy boundary. Review base-model, adapter, and face-encoder licenses and provider retention; consider per-user LoRA only if measured gains justify training and sensitive-artifact lifecycle costs.

Keep [handoff-plan.md](handoff-plan.md) frozen during this follow-up unless the agreed product contract changes; then amend the baseline and bump its version. Delivery status and evaluation outcomes remain on this roadmap, while each other page owns only its assigned facts.

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
5. Resolve the deferred product decisions below.

## Deferred product decisions

These must be decided before production launch. They affect moderation, prompting, consent, data retention, and UX, and must not be left solely to model behavior:

- age bands and content ratings,
- romance and other mature-theme handling,
- irreversible outcomes, including permanent hero death,
- whether users can publish or share stories,
- data retention and export policy,
- whether hero-likeness personalization is available to minors; see the [hero-likeness policy decisions](likeness-consent-policy-review.md).

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

The direction above is anchored in the frozen kickoff baseline documented in [handoff-plan.md](handoff-plan.md). The current product contract is defined in [story-experience.md](story-experience.md).
