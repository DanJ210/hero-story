# Architecture

This document describes the intended target architecture and how the current scaffold maps to it.

## High-level component model

1. **Frontend (Vue 3)** calls API endpoints using bearer auth.
2. **API (.NET 10)** handles auth, session, and scene orchestration.
3. **SQL Server (EF Core)** stores users, sessions, scenes, refresh tokens, and generation jobs.
4. **Azure Queue Storage** buffers image generation work.
5. **Worker (.NET 10 background service)** dequeues jobs and runs image strategies.
6. **Azure Blob Storage** stores generated image outputs and static media assets.
7. **OpenAI services** are used for text generation/moderation and optional image strategy integration.

## Request and processing boundaries

### Synchronous path (user-facing)

- Frontend sends authenticated requests to API.
- API validates identity, performs business logic, persists data, and returns DTOs.
- For scene/image generation, API writes a `GenerationJob` and enqueues payload for async handling.

### Asynchronous path (worker-facing)

- Worker polls queue in batches.
- Each message resolves to a `GenerationJob`.
- Worker updates status transitions (`Pending -> Processing -> Completed/Failed/Poisoned`).
- Output assets are persisted to blob storage and referenced by domain records.

## Security and control surfaces

- JWT bearer authentication with refresh-token workflow.
- Rate limiting policies for auth/session/scene endpoints.
- CORS policy driven by `CORS_ALLOWED_ORIGINS`.
- Middleware for correlation ID and exception handling.
- Content moderation service invoked before creating unsafe content.

## Deployment shape (target)

The architecture is designed for cloud deployment where API and worker are independently scalable compute units over shared SQL/queue/blob backends. The current repository scaffold focuses on local-first development and service boundaries needed for that deployment model.

## Related docs

- Overview: [application-overview.md](application-overview.md)
- API details: [api-summary.md](api-summary.md)
- Data design: [data-model.md](data-model.md)
- Setup and dev workflow: [development-guide.md](development-guide.md)
- Baseline handoff spec: [handoff-plan.md](handoff-plan.md)
