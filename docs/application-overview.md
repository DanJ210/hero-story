# Application overview

Hero Story is an authenticated interactive storytelling MVP where users build narrative sessions and scenes, while asynchronous workers generate or attach scene imagery.

## Purpose

The application demonstrates an end-to-end architecture for:

- user authentication and account lifecycle,
- structured session and scene authoring,
- moderation-aware content generation,
- asynchronous media generation via queue + worker,
- cloud-aligned storage patterns (SQL, queue, blob).

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
   - create/list/read/update/delete user-scoped sessions.
3. **Scene lifecycle**
   - create/list/read scene content associated with sessions.
4. **Image generation jobs**
   - enqueue and process image generation with retry handling.

## How this document relates to other docs

- Architectural boundaries and runtime flow: [architecture.md](architecture.md)
- Endpoint-level API behavior: [api-summary.md](api-summary.md)
- Persistence entities and relationships: [data-model.md](data-model.md)
- Local developer setup and workflows: [development-guide.md](development-guide.md)
- Planned evolution from MVP scaffold: [roadmap.md](roadmap.md)
- Baseline specification source: [handoff-plan.md](handoff-plan.md)
