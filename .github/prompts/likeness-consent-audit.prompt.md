---
name: "Implement Likeness Consent Provenance and Audit"
description: "Implement Hero Story's deferred likeness-consent record and lifecycle audit slice across persistence, API, worker, and tests."
argument-hint: "Optional constraints or confirmed consent-policy decisions"
agent: "agent"
---

Implement the next Hero Story production-readiness slice: replace timestamp-only portrait consent with an explicit consent record and auditable likeness lifecycle.

## Read first

- [Repository instructions](../copilot-instructions.md)
- [Roadmap](../../docs/roadmap.md)
- [Story experience contract](../../docs/story-experience.md)
- [Architecture](../../docs/architecture.md)
- [Data model](../../docs/data-model.md)
- [API summary](../../docs/api-summary.md)
- [Development guide](../../docs/development-guide.md)

Inspect the existing portrait service, portrait and generation-job entities/configuration, worker likeness validation, deletion behavior, migrations, and nearby tests before changing code. Reuse existing audit and persistence patterns where they fit; do not duplicate them.

## Slice requirements

- Persist consent as an explicit, immutable, versioned record that captures the consent purpose, policy version, provider scope, grant time, and the portrait/version it authorizes. Keep source portrait bytes and blob references private; do not put them in queue messages, logs, audit payloads, or public DTOs.
- Update upload, replacement, disablement, deletion, and likeness-use flows to associate actions with the applicable consent record and write append-only audit events. Include the actor and the minimum useful portrait/session/scene/job identifiers. Never store image bytes, unrestricted blob URLs, secrets, or unnecessary prompt content in audit records.
- Make API and worker authorization fail closed unless the portrait is active and the recorded consent is valid for the requested purpose and provider scope. Preserve ownership checks, opt-in defaults, opaque provenance, and current generated-art retention semantics.
- Preserve existing account and portrait deletion guarantees. Handle in-flight likeness jobs consistently with current behavior and prevent stale or revoked consent from reaching the image provider.
- Add the required EF Core migration and focused tests for grant, use, replacement, disablement, deletion, invalid or revoked consent, provenance mismatch, and audit completeness. Reuse existing tests and helpers; avoid a parallel consent or audit subsystem if an existing abstraction can be extended.
- Do not create an export endpoint or claim export support where no real export flow exists. If export auditing cannot be connected to an existing operation, keep that limitation explicit in the roadmap rather than fabricating behavior.

## Product-policy guardrails

- Use only consent wording, purposes, policy versions, provider scopes, retention rules, and minor-access rules already established in the repository or supplied as invocation arguments.
- Do not invent legal or product policy defaults. If a required decision is missing, identify the specific blocker before encoding a default; implement only the portions that remain valid without that decision.
- Keep user consent explicit and revocable. Do not infer sensitive traits from portraits or broaden the authorized use beyond the recorded scope.

## Validation and handoff

- Run focused unit and integration tests for the changed API, infrastructure, and worker behavior, then run the applicable project suites documented in [Development guide](../../docs/development-guide.md).
- Because integration tests use EF InMemory, validate the migration and persistence mapping with the SQL Server provider as well. Follow the repository's clean-environment guidance before rebuilding or restarting API/worker processes.
- Validate the actual likeness request path through API consent/provenance checks, persistence, and worker provider-boundary enforcement. Include frontend tests/type checks only if frontend behavior or contracts change.
- Update the owning docs to match verified behavior: [roadmap](../../docs/roadmap.md) for status, [data model](../../docs/data-model.md) for entities/migrations, [architecture](../../docs/architecture.md) for enforcement boundaries, [API summary](../../docs/api-summary.md) for changed contracts, and [development guide](../../docs/development-guide.md) for new configuration or commands.
- Report changed files, migration name, tests and commands run, SQL Server validation result, policy decisions still open, and any behavior intentionally deferred. Do not commit changes.