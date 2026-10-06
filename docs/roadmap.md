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
- Hero-likeness privacy and release gates remain open; the product-level preview decisions and draft wording are documented in [story-experience.md](story-experience.md#hero-preview-and-approval) and [likeness-consent-policy-review.md](likeness-consent-policy-review.md):
  1. Obtain privacy-owner approval for the separate preview-consent notice and links, then determine its policy version and fresh-grant requirements.
  2. Verify the effective provider, model, edit operation, and account/project controls. Keep preview consent scoped to the verified operation; any provider, model, or purpose change needs a distinct scope and fresh consent.
  3. Keep general availability disabled until an enforceable age policy exists. Block adult-volunteer evaluation until privacy-approved age verification and consent procedures exist; account data does not establish age.
  4. Confirm that provider-side handling, retention, deletion, and the in-flight request boundary are acceptable for the exact account and operation. App deletion cannot recall data already sent to a provider.
  5. Verify the deployment's maximum backup-expiry window for disclosure, and approve a minimized consent/audit-record retention schedule with restricted access.
  6. Revisit export auditing when an export operation exists; there is no export endpoint or event to audit yet.

## Near-term (MVP completion)

- [x] Add vertical-slice tests proving automatic artwork retry and completed-job idempotency.

### Proposed likeness-quality workflow and documentation alignment

The consent-driven likeness path above does not establish visual-quality acceptance. This follow-up is proposed work, not shipped preview, approval, or multi-reference behavior. Start with the configured OpenAI image strategy; do not change providers or introduce per-user training before a bounded quality evaluation. Product decisions for preview consent, visibility, regeneration limits, approval binding, invalidation, appearance catalog, derived-preview deletion, and temporary adult-evaluation eligibility are recorded in [story-experience.md](story-experience.md#hero-preview-and-approval). Those decisions do not establish implementation status.

- [ ] Complete preview readiness: privacy-owner approval of separate preview-consent wording and policy version; verification of the effective model/account operation and provider data controls; an enforceable adult-evaluation eligibility workflow; and a verified maximum backup-expiry window for disclosure. The repository does not establish the deployed backup-expiry period or the evaluation/deployment account's data-retention setting.
- [ ] Implement the private preview-and-approval slice under the decisions in [story-experience.md](story-experience.md#hero-preview-and-approval). Its fixed v1 catalog and defaults are agreed; preview and story-artwork consent remain distinct.
- [ ] Run a bounded evaluation using five consenting adult volunteers and six scene types per person (close-up, profile, full-body, action, low light, and multiple people) as proposed targets. For each of 30 volunteer/scene cases, compare portrait-only with portrait-plus-approved-hero output using the same fixed prompt; hide arm/order from raters where practical. This yields 60 first outputs and at most 180 outputs with a maximum of two retries per case/arm. Participants rate likeness, costume consistency, and scene accuracy separately on 1–5 scales and mark each image usable/not usable; record latency and cost from system/provider data. Define first-output usable as accepted without regeneration, report results overall and by scene type as directional for this small sample, and report cost per accepted image including retries. The proposed targets of at least 80% accepted without regeneration and mean likeness of at least 4/5 are not requirements or measured results. Keep adult recruitment blocked until the age-verification and consent workflow is approved.
- [ ] Deliver the private preview-and-approval slice before adding approved references to story artwork. Follow the agreed consent, visibility, request-limit, approval-versioning, invalidation, replacement/disablement/deletion, retention, and stale-job rules in [story-experience.md](story-experience.md#hero-preview-and-approval).
- [ ] Deliver persistent visual identity and multi-reference scene generation as a subsequent slice. Keep the original portrait as the identity anchor rather than chaining only generated scenes. OpenAI documentation for the configured development model (`gpt-image-1`) describes `/v1/images/edits`, up to 16 GPT Image inputs, and optional high/low `input_fidelity`; the existing request uses one reference and omits that option. Verify the effective configured model, project permissions/settings, request behavior, and measured identity/cost/latency quality before relying on the capability. API documentation does not establish multi-reference likeness fidelity.
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
