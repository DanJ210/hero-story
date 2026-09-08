# Documentation index

Hero Story is an authenticated, serialized superhero-story experience where the reader is the protagonist. It uses a conversational interaction loop while presenting generated content as continuous, book-like prose.

This page is the entry point to `docs/`. Each fact has exactly one home; use the table below to find it rather than looking for a summary here.

## Where to look

| You want | Read |
| --- | --- |
| What the product promises, the turn contract, revision, artwork, and likeness policy | [story-experience.md](story-experience.md) |
| What is done, in progress, or deferred | [roadmap.md](roadmap.md) |
| How components fit together and how a request flows | [architecture.md](architecture.md) |
| Routes, DTO fields, status codes, and error contracts | [api-summary.md](api-summary.md) |
| Entities, relationships, and migrations | [data-model.md](data-model.md) |
| Local setup, configuration keys, and commands | [development-guide.md](development-guide.md) |
| The frozen kickoff baseline, for detecting scope drift | [handoff-plan.md](handoff-plan.md) |

## Functional domains

1. **Identity and access**
   - register, login, refresh, logout, delete account.
2. **Story sessions**
   - create/list/read/update/delete user-scoped hero stories and track the active episode.
3. **Scene lifecycle**
   - accept a user contribution and produce the next narrative turn on the active story path, with suggestions, continuity state, non-destructive latest-turn revision, and episode completion.
4. **Image generation jobs**
   - enqueue and process selected story-beat artwork with retry and poison handling.
5. **Hero-likeness personalization**
   - consent-gated private portrait upload, replacement, disablement, and deletion, with opaque provenance on likeness artwork jobs.

## Repository layout

- `src/HeroStory.Api` — controllers, DTOs, services, middleware
- `src/HeroStory.Core` — domain entities and enums
- `src/HeroStory.Infrastructure` — EF Core, Azure clients, storage, OpenAI helpers
- `src/HeroStory.Worker` — queue processing and image strategies
- `src/HeroStory.Frontend` — Vue 3 SPA; canonical source under its nested `src/`
- `tests/` — xUnit unit and integration projects
