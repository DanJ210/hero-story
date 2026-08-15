# Hero Story

Hero Story is a serialized superhero story where the reader is the main character. The experience uses a conversational loop: the application presents a book-like passage, the user decides what their hero says or does, and the next passage reflects that decision. The repository currently contains a scaffolded MVP across API, worker, frontend, core domain, infrastructure, and tests.

## Project overview

The MVP is split into three runtime services:

1. **API (`HeroStory.Api`)** exposes auth, story session, scene, and generation job endpoints.
2. **Worker (`HeroStory.Worker`)** polls queue messages and runs image generation strategies.
3. **Frontend (`HeroStory.Frontend`)** provides a Vue 3 interface for auth and the reader-first conversational story flow.

Supporting projects include domain entities in `HeroStory.Core`, infrastructure adapters in `HeroStory.Infrastructure`, and unit/integration tests under `tests/`.

For deeper detail, start with [docs/application-overview.md](docs/application-overview.md).

The target experience, turn contract, revision behavior, and MVP acceptance criteria are defined in [docs/story-experience.md](docs/story-experience.md).

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
   - Copy `src/HeroStory.Frontend/.env.example` to `src/HeroStory.Frontend/.env`.
   - Non-secret .NET development settings are committed in each project's `appsettings.Development.json`.
   - Store the SQL connection string in both .NET projects with `dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=HeroStoryDb;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True" --project <PROJECT_PATH>`.
   - Store `JWT_SECRET` and `OPENAI_API_KEY` in the API project's user secrets. Never put real secrets in committed appsettings files.
   - Environment variables remain supported as higher-precedence overrides. The root `.env` is read automatically by Docker Compose, but not by `dotnet run`.
2. **Start local dependencies with Docker Compose**
   - Start SQL Server and Azurite with `docker compose up -d`, or reuse existing services on ports `1433` and `10000`-`10002`.
   - When using this repository's Compose stack, copy `.env.example` to `.env`; Compose reads `${MSSQL_SA_PASSWORD}` and `${ACCEPT_EULA}` from it.
   - When reusing an existing SQL container, use the password that initialized that container in both projects' `ConnectionStrings:Default` user secret. Changing `.env` does not reset an existing container password.
   - Stop the dependencies with `docker compose down`; named volumes preserve local data between starts.
3. **Run API**
   - `dotnet run --project src/HeroStory.Api`
   - With `DB_APPLY_MIGRATIONS=true`, the API applies committed EF Core migrations during startup and fails immediately if the SQL connection is invalid.
4. **Run Worker**
   - `dotnet run --project src/HeroStory.Worker`
5. **Run Frontend**
   - `npm --prefix src/HeroStory.Frontend install`
   - `npm --prefix src/HeroStory.Frontend run dev`
   - Vite reads `src/HeroStory.Frontend/.env`; the root `.env` is not visible to the frontend.

In Development, the login page also shows **Continue as development user**. This calls `POST /api/auth/dev-login`, creates or reuses the configured development user, and issues normal JWT and refresh tokens. The API route is mapped only when the host environment is `Development` and `DEV_AUTH_ENABLED=true`; it is unavailable in Testing and Production.

Additional setup and troubleshooting notes are in [docs/development-guide.md](docs/development-guide.md).

## Core request flows

### Auth flow

1. User registers or logs in via `POST /api/auth/register` or `POST /api/auth/login`.
2. API validates credentials through `AuthService` and returns JWT + refresh token payload.
3. Frontend stores auth state and includes bearer tokens in API requests.

### Scene generation flow

1. Authenticated user creates a story session through `POST /api/sessions`.
2. User submits what their hero says, attempts, or chooses through `POST /api/sessions/{id}/scenes`.
3. API moderates the contribution, advances the narrative, and returns the next story turn.
4. API validates and stores structured narrative output, including summary, location, active conflict, schema-versioned state, 2–3 optional suggested actions, story beat, and episode-completion status.
5. Continuity-aware prompting, meaningful cross-turn consequences, latest-turn revision, session episode transitions, and selective artwork remain planned. The current implementation still creates an image job for every scene.

### Worker image pipeline

1. Worker polls the configured Azure Queue on interval.
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
- [Interactive story experience](docs/story-experience.md)
- [Handoff plan baseline](docs/handoff-plan.md)
- [Copilot instructions](.github/copilot-instructions.md)
