# Copilot instructions for Hero Story

## Repository purpose

This repository is the Hero Story MVP: a story-generation application with:
- ASP.NET Core API for auth, sessions, scenes, and jobs
- Vue 3 frontend for authenticated user flows
- .NET worker for asynchronous image generation
- SQL Server persistence via EF Core
- Azure Queue and Blob storage integration
- OpenAI text generation and moderation

The repository is intended to support incremental development from scaffold to production-ready MVP. Keep implementation choices aligned with the baseline in `docs/handoff-plan.md`, the product contract in `docs/story-experience.md`, and the architecture in `docs/architecture.md`.

## Core stack

- .NET 10 / ASP.NET Core 10
- EF Core 10 + ASP.NET Identity
- Vue 3 + Vite + TypeScript + Pinia + Vue Router
- Azure SQL / SQL Server, Azure Queue Storage, Azure Blob Storage
- OpenAI Chat Completions and Moderation APIs
- xUnit and Vitest for automated validation

## Working assumptions

- Prefer the target design in the handoff plan over ad hoc shortcuts.
- Keep all code and comments in English.
- Never commit secrets or real credentials. Use .NET user secrets for API/worker secrets, the ignored root `.env` only for Docker Compose values, and `src/HeroStory.Frontend/.env` only for non-secret Vite settings. Keep placeholders in the corresponding `.env.example` files.
- Preserve the user-facing intent of the app: safe, scoped, moderated storytelling flows with secure auth and queue-based image processing.
- Preserve the reader-first product contract: the user is the superhero, prose is book-like, free-text decisions materially influence stored story state, suggestions remain optional, and revision preserves prior versions rather than destructively rewriting history.
- Treat the repo as a real project with a baseline scaffold; do not remove required project structure, source folders, or documentation.

## Project structure

- `src/HeroStory.Api/` — API endpoints, auth, DTOs, services, middleware
- `src/HeroStory.Core/` — domain entities and enums
- `src/HeroStory.Infrastructure/` — EF Core, Azure clients, storage, OpenAI helpers
- `src/HeroStory.Worker/` — background job processing and image strategy implementation
- `src/HeroStory.Frontend/` — Vite project root; canonical Vue/TypeScript source is under its nested `src/` directory
- `tests/` — .NET unit and integration test projects; frontend tests use Vitest when test files exist
- `docs/` — design, architecture, API, data model, roadmap, and handoff plan

## Coding expectations

- Keep implementations consistent with existing project naming, folder structure, and .NET conventions.
- Prefer explicit types and small service boundaries.
- Put non-secret local .NET defaults in the owning project's `appsettings.Development.json`; put secrets in that project's user-secrets store. Environment variables are supported as higher-precedence overrides.
- Keep .NET 10 platform package versions aligned across API, Core, Infrastructure, Worker, and test projects. In particular, do not mix EF Core or ASP.NET Core major versions.
- Treat `.ts` and `.vue` files under `src/HeroStory.Frontend/src` as canonical source. Keep TypeScript `noEmit` enabled and never add generated `.js` files beside source; Vite output belongs in ignored `dist/`.
- For OpenAI or external service calls, fail safely and surface structured errors rather than leaking secrets or sensitive data.
- If a feature is not fully implemented, preserve a clear TODO or stub with explanatory comment rather than leaving broken behavior silently.
- Prefer secure defaults: validate input, enforce auth ownership checks, and avoid exposing raw errors to the client.

## Testing expectations

- Run the smallest relevant validation command for the change.
- Use existing test projects rather than creating new bespoke tooling.
- Prefer targeted validation before broad suites when iterating on a single feature area.
- Integration tests replace SQL Server with EF InMemory. When changing EF registrations, remove all production provider option registrations, including `IDbContextOptionsConfiguration<AppDbContext>`, before adding InMemory.
- Stop a running `HeroStory.Api` process before builds that rebuild its project reference; on Windows it locks `HeroStory.Api.exe` and causes `MSB3021`/`MSB3027` copy failures.
- Preserve buildability and testability across API, worker, and frontend layers.

## Commands

Typical repository commands:

- `dotnet restore hero-story.sln`
- `dotnet build hero-story.sln`
- `dotnet test hero-story.sln --no-build`
- `npm --prefix src/HeroStory.Frontend install`
- `npm --prefix src/HeroStory.Frontend run build`
- `npm --prefix src/HeroStory.Frontend run dev`

## Development-only behavior

- The development auth shortcut must remain guarded by both the `Development` host environment and `DEV_AUTH_ENABLED=true` on the API. The frontend flag controls button visibility only and is not a security boundary.
- Development auth must issue normal JWT and refresh tokens for a persisted development user so authorization and ownership checks remain exercised. Never weaken or replace production authentication to support local testing.
- `DB_APPLY_MIGRATIONS=true` applies committed EF Core migrations during API startup. Add a migration when the persistent model changes and validate it with the SQL Server provider.

## Documentation guidance

- Keep README and docs updated when architecture, workflows, or setup materially change.
- When adding or changing behavior, update the relevant docs page in `docs/` and link from the README if needed.
- Use the handoff package in `docs/handoff-plan.md` as the baseline product and engineering specification.

## Non-goals

- Do not create fake or placeholder functionality and present it as completed production work.
- Do not introduce dependency or framework drift without updating documentation and repository expectations.
- Do not bypass auth, moderation, ownership, or privacy requirements from the product plan. Development-only shortcuts must preserve those downstream controls and be impossible to map outside Development.
