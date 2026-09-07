---
description: "Apply when editing any file under docs/ or the README: which doc owns which fact, and the phrasing rules that keep delivery status from drifting."
applyTo: "docs/**,README.md"
---

# Documentation consistency rules

## One home per fact

Every fact has exactly one owning page. Other pages link to it instead of restating it. Restating is what causes drift.

| Fact | Owning page |
| --- | --- |
| Delivery status: complete, in progress, deferred | `docs/roadmap.md` |
| Product promise, turn contract, revision, artwork, likeness policy | `docs/story-experience.md` |
| Deferred product policy decisions | `docs/roadmap.md` |
| Component structure, request flow, trust and privacy boundaries | `docs/architecture.md` |
| Routes, DTO fields, query parameters, status codes, error contracts | `docs/api-summary.md` |
| Entities, relationships, migrations | `docs/data-model.md` |
| Setup steps, configuration keys, secrets, commands | `docs/development-guide.md` |
| Doc index and functional domains | `docs/application-overview.md` |
| Original kickoff scope, for detecting drift | `docs/handoff-plan.md` |

## Phrasing rules

- Only `docs/roadmap.md` may make completion claims. Everywhere else, describe behavior in plain present tense.
- Outside the roadmap, do not write "currently", "now", "implemented", "not yet", "planned", "target behavior", "remains roadmap work", or "the current implementation". A page written without those words cannot go stale about status.
- Write "The worker validates provenance", not "The worker now validates provenance" or "Provenance validation is implemented".
- Never describe a capability as shipped until it is verifiable in code: the route, the service, the entity or migration, the worker path, or the frontend component.
- If a capability is partially delivered, put the split in the roadmap. Do not hedge in the implementation docs.

## Page-specific rules

- `docs/handoff-plan.md` is a frozen baseline. Amend it only when the product contract itself changes, and bump its version when you do. Never use it to track slice progress.
- `docs/application-overview.md` is an index, not a summary. Do not add a narrative recap of the product or its progress.
- `README.md` orients a newcomer and links out. It carries no status narrative.

## Before finishing a doc edit

1. Every relative link still resolves.
2. No page contradicts another.
3. No status claim was added outside the roadmap.
4. Any capability described as shipped was checked against the code, not against another doc.
