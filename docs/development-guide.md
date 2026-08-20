# Development guide

## Prerequisites

- .NET SDK 10 (from `global.json`)
- Node.js (for Vue frontend)
- Docker Desktop (for local SQL/Azurite dependencies)
- Optional: Azure CLI and Azurite tooling if running storage services outside Docker

## Environment configuration

The .NET hosts load configuration in the normal order: base appsettings, `appsettings.Development.json`, user secrets, then environment variables. Later sources override earlier sources.

1. Copy `src/HeroStory.Frontend/.env.example` to `src/HeroStory.Frontend/.env`.
2. Configure the API secrets:
   - `dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=HeroStoryDb;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True" --project src/HeroStory.Api`
   - `dotnet user-secrets set "JWT_SECRET" "<32-BYTE-OR-LONGER-SECRET>" --project src/HeroStory.Api`
   - `dotnet user-secrets set "OPENAI_API_KEY" "<OPENAI-KEY>" --project src/HeroStory.Api`
3. Configure the worker SQL secret:
   - `dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=HeroStoryDb;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True" --project src/HeroStory.Worker`
4. Review non-secret values in both projects' `appsettings.Development.json` and the frontend `.env`.
5. Copy `.env.example` to `.env` only when using this repository's Docker Compose stack or environment-variable overrides.

When sharing an existing SQL Server container, `<SQL_PASSWORD>` is the password used when that container was first created. `MSSQL_SA_PASSWORD` initializes a new Compose container but does not reset the password of an existing container.

## Running services locally

Start the committed SQL Server and Azurite stack, or reuse compatible services already listening on SQL port `1433` and Azurite ports `10000`-`10002`:

1. `docker compose up -d`
2. `docker compose ps`

Compose automatically reads the root `.env` file for its `${MSSQL_SA_PASSWORD}` and `${ACCEPT_EULA}` values. The named volumes preserve local database and storage data between starts. Stop the dependencies with `docker compose down` when finished.

Then run the application services directly:

1. API: `dotnet run --project src/HeroStory.Api`
2. Worker: `dotnet run --project src/HeroStory.Worker`
3. Frontend:
   - `npm --prefix src/HeroStory.Frontend install`
   - `npm --prefix src/HeroStory.Frontend run dev`

With `DB_APPLY_MIGRATIONS=true`, API startup applies the committed EF Core migrations. A bad SQL login or unavailable server therefore fails during startup instead of surfacing later as a request-time error.

Creating a story calls OpenAI moderation and chat generation immediately to produce the opening passage. The configured API key must have access to `omni-moderation-latest`, the configured text model, and available quota. Provider rate-limit or quota failures return `503 Service Unavailable`, and the incomplete session is removed rather than left in the user's story list.

Moderation is category-aware rather than using the provider's overall `flagged` result. Superhero and sci-fi action routinely trips the `violence` category, so `violence` and non-threatening `harassment` do not block; `sexual`, `sexual/minors`, `hate`, `hate/threatening`, `harassment/threatening`, `self-harm*`, `illicit*`, and `violence/graphic` do. A flagged response with no categories blocks as `unspecified`. Override the blocking set with the comma-separated `MODERATION_BLOCKED_CATEGORIES` setting. Blocked input is rejected; blocked output is replaced with a short safe passage and stored as `Sanitized` with the matching categories in `ModerationDetail`.

## Development authentication

The Development profile enables a temporary authentication shortcut for exercising authenticated application flows without registering or entering a password:

1. Keep `DEV_AUTH_ENABLED=true` in the API's `appsettings.Development.json`.
2. Keep `VITE_DEV_AUTH_ENABLED=true` in `src/HeroStory.Frontend/.env` to show the development-login button.
3. Start the API and frontend, then select **Continue as development user** on the login page.

The endpoint creates or reuses the user configured by `DEV_AUTH_EMAIL` and issues the same JWT and refresh-token format as normal login. The API maps `POST /api/auth/dev-login` only when both the host environment is `Development` and the feature flag is enabled. The frontend flag controls visibility only; the API environment and flag are the security boundary.

## Frontend TypeScript output

- TypeScript and Vue source files under `src/HeroStory.Frontend/src` are the canonical source.
- `tsconfig.json` sets `noEmit`, so type checking does not create adjacent `.js` files.
- Vite transpiles and bundles production JavaScript into `src/HeroStory.Frontend/dist`, which is ignored by Git.

## Running tests

- Unit tests: `dotnet test tests/HeroStory.UnitTests/HeroStory.UnitTests.csproj`
- Integration tests: `dotnet test tests/HeroStory.IntegrationTests/HeroStory.IntegrationTests.csproj`

## Scaffold conventions

- API business logic lives in services, not controllers.
- Infrastructure adapters are in `HeroStory.Infrastructure`.
- Worker strategy selection is controlled by `IMAGE_STRATEGY` (for example `placeholder` or `dalle3`).
- Queue retries and poison handling are controlled through `AZURE_QUEUE_*` settings.

## Documentation workflow

When adding or changing functionality:

1. Update endpoint or flow details in [api-summary.md](api-summary.md).
2. Update architecture or data changes in [architecture.md](architecture.md) and [data-model.md](data-model.md).
3. Keep README index links in sync.

## Related docs

- Project orientation: [application-overview.md](application-overview.md)
- Long-term direction: [roadmap.md](roadmap.md)
