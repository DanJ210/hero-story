# Application overview

Hero Story is an authenticated, serialized superhero-story experience where the reader is the protagonist. It uses a conversational interaction loop while presenting generated content as continuous, book-like prose.

## Purpose

The application demonstrates an end-to-end architecture for:

- user authentication and account lifecycle,
- hero and episode creation,
- free-text user actions plus optional suggested actions,
- continuity-aware narrative turns shaped by user decisions,
- non-destructive latest-turn revision,
- moderation-aware content generation,
- selective asynchronous artwork for major story beats,
- cloud-aligned storage patterns (SQL, queue, blob).

The product and acceptance contract is defined in [story-experience.md](story-experience.md). The current implementation persists validated structured turn output and feeds the latest accepted turn back into the next bounded prompt. Revision lineage, active-path reads, session episode transitions, selective artwork, and multi-turn summary compaction remain roadmap work.

## Current scaffold status

The repository currently includes:

- API controllers for auth, sessions, scenes, and generation jobs,
- domain entities and EF Core data model wiring,
- queue and blob infrastructure adapters,
- worker pipeline for dequeue/process/retry/poison behavior,
- Vue 3 frontend pages and stores for core interaction paths,
- unit and integration test scaffolding for key service and endpoint behavior.

Some production-hardening and deployment assets are still roadmap items. See [roadmap.md](roadmap.md).

## Functional domains

1. **Identity and access**
   - register, login, refresh, logout, delete account.
2. **Story sessions**
   - create/list/read/update/delete user-scoped hero stories and track the active episode.
3. **Scene lifecycle**
   - accept a user contribution and produce the next narrative turn on the active story path.
   - target behavior supports suggestions, continuity state, latest-turn revision, and episode completion.
4. **Image generation jobs**
   - enqueue and process selected story-beat artwork with retry handling.

## How this document relates to other docs

- Architectural boundaries and runtime flow: [architecture.md](architecture.md)
- Endpoint-level API behavior: [api-summary.md](api-summary.md)
- Persistence entities and relationships: [data-model.md](data-model.md)
- Local developer setup and workflows: [development-guide.md](development-guide.md)
- Planned evolution from MVP scaffold: [roadmap.md](roadmap.md)
- Product, turn, and revision contract: [story-experience.md](story-experience.md)
- Baseline specification source: [handoff-plan.md](handoff-plan.md)
