# API summary

Base route prefix: `api/`

The API is implemented in `src/HeroStory.Api` using controller-based endpoints and DTO contracts.

## Authentication endpoints (`/api/auth`)

- `POST /api/auth/register`
  - Creates user account.
  - Returns `201 Created` with registration response.
- `POST /api/auth/login`
  - Authenticates user credentials.
  - Returns token payload (access + refresh).
- `POST /api/auth/refresh`
  - Exchanges refresh token for new token payload.
- `POST /api/auth/logout`
  - Revokes refresh token context.
  - Returns `204 No Content`.
- `DELETE /api/auth/account`
  - Authenticated account deletion request.
  - Returns `202 Accepted`.
## Story session endpoints (`/api/sessions`)

- `GET /api/sessions`
  - Lists current user's sessions.
- `POST /api/sessions`
  - Creates a session.
  - Returns `201 Created`.
- `GET /api/sessions/{id}`
  - Gets single session.
  - Returns `404` if not found/user-mismatched.
- `PATCH /api/sessions/{id}`
  - Updates mutable session state.
- `DELETE /api/sessions/{id}`
  - Soft-deletes/marks session removal.

## Scene endpoints (`/api/sessions/{id}/scenes`)

- `GET /api/sessions/{id}/scenes`
  - Lists scenes for session.
- `POST /api/sessions/{id}/scenes`
  - Creates a scene and triggers generation workflow.
  - Returns `201 Created`.
- `GET /api/sessions/{id}/scenes/{sceneId}`
  - Retrieves scene detail.

## Generation jobs (`/api/jobs`)

- `GET /api/jobs/{jobId}`
  - Retrieves a single generation job (status, attempts, error detail).
  - Returns `404` if not found.
## Cross-cutting behavior

- JWT bearer auth is required except on allow-anonymous auth endpoints.
- Rate limiter policies are configured for register, login, sessions, and scenes flows.
- JSON contract uses camelCase.
- Exception middleware returns normalized error responses.

## Related docs

- Runtime architecture: [architecture.md](architecture.md)
- Entity model backing these endpoints: [data-model.md](data-model.md)
