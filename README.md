# Hero Story

Hero Story is an interactive storytelling application where authenticated users create story sessions, generate scenes, and queue background image generation for each scene. The repository currently contains a scaffolded MVP across API, worker, frontend, core domain, infrastructure, and tests, with Azure-compatible integrations configured for local development defaults.

## Project overview

The MVP is split into three runtime services:

1. **API (`HeroStory.Api`)** exposes auth, story session, scene, and generation job endpoints.
2. **Worker (`HeroStory.Worker`)** polls queue messages and runs image generation strategies.
3. **Frontend (`HeroStory.Frontend`)** provides a Vue 3 interface for auth and story/session flows.

Supporting projects include domain entities in `HeroStory.Core`, infrastructure adapters in `HeroStory.Infrastructure`, and unit/integration tests under `tests/`.

For deeper detail, start with [docs/application-overview.md](docs/application-overview.md).

## Architecture summary

The target architecture and current scaffold are aligned around these components:

- **API**: ASP.NET Core app with JWT auth, rate limiting, CORS, and controller-based endpoints.
- **Worker**: .NET background service processing queued image jobs.
- **Frontend**: Vue 3 + Vite SPA with API clients and route-based pages.
- **SQL**: SQL Server persistence via EF Core `AppDbContext` (falls back to in-memory when no SQL connection string is provided in API).
- **Queue**: Azure Queue Storage for image job dispatch + poison queue.
- **Blob**: Azure Blob Storage for generated and asset images with SAS URL support.

See [docs/architecture.md](docs/architecture.md) for runtime interactions and boundaries.

## Tech stack

- **Backend**: .NET 10, ASP.NET Core, EF Core, ASP.NET Identity, JWT bearer auth
- **Frontend**: Vue 3, Vite, TypeScript, Pinia
- **Data and messaging**: SQL Server, Azure Queue Storage, Azure Blob Storage
- **AI services**: OpenAI text generation/moderation and worker strategy support for DALL·E-style image generation
- **Testing**: xUnit unit and integration test projects

See [docs/development-guide.md](docs/development-guide.md) for toolchain expectations.

## Local setup quickstart

1. **Create local environment file**
   - Copy `.env.example` to `.env`.
   - Replace placeholders and set SQL connection strings: `JWT_SECRET`, `OPENAI_API_KEY`, and ensure both `SQLSERVER_CONNECTION_STRING` (API) and `DB_CONNECTION_STRING` (worker) point to the same SQL Server instance.
2. **Start local dependencies with Docker Compose**
   - This repository currently does not include a committed `docker-compose.yml`; use your local/standard stack for SQL Server + Azurite with values matching `.env`.
3. **Run API**
   - `dotnet run --project src/HeroStory.Api`
4. **Run Worker**
   - `dotnet run --project src/HeroStory.Worker`
5. **Run Frontend**
   - `npm install --prefix src/HeroStory.Frontend`
   - `npm run dev --prefix src/HeroStory.Frontend`

Additional setup and troubleshooting notes are in [docs/development-guide.md](docs/development-guide.md).

## Core request flows

### Auth flow

1. User registers or logs in via `POST /api/auth/register` or `POST /api/auth/login`.
2. API validates credentials through `AuthService` and returns JWT + refresh token payload.
3. Frontend stores auth state and includes bearer tokens in API requests.

### Scene generation flow

1. Authenticated user creates a story session through `POST /api/sessions`.
2. User submits scene creation through `POST /api/sessions/{id}/scenes`.
3. API runs moderation + scene generation logic and returns scene data.
4. API enqueues image generation job metadata for asynchronous processing.

### Worker image pipeline

1. Worker polls Azure Queue (`image-generation-jobs`) on interval.
2. Worker marks matching `GenerationJob` row as processing and increments attempts.
3. Selected image strategy (`placeholder` or `dalle3`) generates an image and writes blob content.
4. Worker updates job status and deletes queue message, or moves exhausted failures to poison queue.

See [docs/api-summary.md](docs/api-summary.md) and [docs/architecture.md](docs/architecture.md) for endpoint and pipeline detail.

## Repository structure map

```text
.
├─ src/
│  ├─ HeroStory.Api/            # ASP.NET Core API (controllers, services, middleware)
│  ├─ HeroStory.Worker/         # Background queue processor for image jobs
│  ├─ HeroStory.Frontend/       # Vue 3 frontend (pages, router, stores, API clients)
│  ├─ HeroStory.Core/           # Domain entities and enums
│  └─ HeroStory.Infrastructure/ # EF Core DbContext, queue/blob/OpenAI clients
├─ tests/
│  ├─ HeroStory.UnitTests/
│  └─ HeroStory.IntegrationTests/
├─ docs/                        # Application and architecture documentation
├─ .env.example                 # Local configuration template
└─ hero-story.sln               # Solution entry point
```

## Documentation index

- [Application overview](docs/application-overview.md)
- [Architecture](docs/architecture.md)
- [API summary](docs/api-summary.md)
- [Data model](docs/data-model.md)
- [Development guide](docs/development-guide.md)
- [Roadmap](docs/roadmap.md)
- [Handoff plan baseline](docs/handoff-plan.md)
- [Copilot instructions](.github/copilot-instructions.md)
