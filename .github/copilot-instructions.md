# Copilot instructions for Hero Story

## Repository purpose

This repository is the Hero Story MVP: a story-generation application with:
- ASP.NET Core API for auth, sessions, scenes, and jobs
- Vue 3 frontend for authenticated user flows
- .NET worker for asynchronous image generation
- SQL Server persistence via EF Core
- Azure Queue and Blob storage integration
- OpenAI text generation and moderation

The repository is intended to support incremental development from scaffold to production-ready MVP. Keep implementation choices aligned with the handoff package in `docs/handoff-plan.md` and the architecture docs in `docs/architecture.md`.

## Core stack

- .NET 10 / ASP.NET Core
- EF Core + ASP.NET Identity
- Vue 3 + Vite + TypeScript + Pinia + Vue Router
- Azure SQL / SQL Server, Azure Queue Storage, Azure Blob Storage
- OpenAI Chat Completions and Moderation APIs
- xUnit and Vitest for automated validation

## Working assumptions

- Prefer the target design in the handoff plan over ad hoc shortcuts.
- Keep all code and comments in English.
- Never commit secrets or real credentials. Use `.env` locally and keep placeholders only in `.env.example`.
- Preserve the user-facing intent of the app: safe, scoped, moderated storytelling flows with secure auth and queue-based image processing.
- Treat the repo as a real project with a baseline scaffold; do not remove required project structure, source folders, or documentation.

## Project structure

- `src/HeroStory.Api/` — API endpoints, auth, DTOs, services, middleware
- `src/HeroStory.Core/` — domain entities and enums
- `src/HeroStory.Infrastructure/` — EF Core, Azure clients, storage, OpenAI helpers
- `src/HeroStory.Worker/` — background job processing and image strategy implementation
- `src/HeroStory.Frontend/` — Vue 3 app and client-side state
- `tests/` — unit, integration, and frontend test projects
- `docs/` — design, architecture, API, data model, roadmap, and handoff plan

## Coding expectations

- Keep implementations consistent with existing project naming, folder structure, and .NET conventions.
- Prefer explicit types and small service boundaries.
- Do not add hardcoded configuration values where env vars or app config should be used.
- For OpenAI or external service calls, fail safely and surface structured errors rather than leaking secrets or sensitive data.
- If a feature is not fully implemented, preserve a clear TODO or stub with explanatory comment rather than leaving broken behavior silently.
- Prefer secure defaults: validate input, enforce auth ownership checks, and avoid exposing raw errors to the client.

## Testing expectations

- Run the smallest relevant validation command for the change.
- Use existing test projects rather than creating new bespoke tooling.
- Prefer targeted validation before broad suites when iterating on a single feature area.
- Preserve buildability and testability across API, worker, and frontend layers.

## Commands

Typical repository commands:

- `dotnet restore hero-story.sln`
- `dotnet build hero-story.sln`
- `dotnet test hero-story.sln --no-build`
- `npm install --prefix src/HeroStory.Frontend`
- `npm run dev --prefix src/HeroStory.Frontend`

## Documentation guidance

- Keep README and docs updated when architecture, workflows, or setup materially change.
- When adding or changing behavior, update the relevant docs page in `docs/` and link from the README if needed.
- Use the handoff package in `docs/handoff-plan.md` as the baseline product and engineering specification.

## Non-goals

- Do not create fake or placeholder functionality and present it as completed production work.
- Do not introduce dependency or framework drift without updating documentation and repository expectations.
- Do not bypass auth, moderation, or privacy requirements from the product plan.
