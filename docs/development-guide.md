# Development guide

## Prerequisites

- .NET SDK 10 (from `global.json`)
- Node.js (for Vue frontend)
- Docker Desktop (for local SQL/Azurite dependencies)
- Optional: Azure CLI and Azurite tooling if running storage services outside Docker

## Environment configuration

1. Copy `.env.example` to `.env`.
2. Populate secrets and local values:
   - `JWT_SECRET`
   - `OPENAI_API_KEY`
   - `MSSQL_SA_PASSWORD`
   - storage connection strings and queue/container names
3. Ensure API/worker/frontend URLs are aligned (`JWT_ISSUER`, `JWT_AUDIENCE`, `VITE_API_BASE_URL`).

## Running services locally

Because a `docker-compose.yml` file is not currently committed, use your local compose stack or equivalent containers for SQL Server + Azurite, then run application services directly:

1. API: `dotnet run --project src/HeroStory.Api`
2. Worker: `dotnet run --project src/HeroStory.Worker`
3. Frontend:
   - `npm install --prefix src/HeroStory.Frontend`
   - `npm run dev --prefix src/HeroStory.Frontend`

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
