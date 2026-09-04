---
applyTo: "src/**,tests/**"
description: "Use when performing a code review on Hero Story pull requests or local changes: how to validate the change in the prepared environment and which correctness, security, and architecture checks to apply."
---

# Hero Story code review instructions

## Environment

The review environment is prepared by `.github/workflows/copilot-code-review.yml`: the .NET SDK from `global.json`, a restored and built solution, and installed frontend dependencies. Assume those steps already ran; do not re-install toolchains.

Use these commands to verify a change instead of reasoning about behavior from the diff alone:

- `dotnet build hero-story.sln` — backend compilation after C# changes
- `dotnet test hero-story.sln --no-build` — unit and integration tests
- `npm --prefix src/HeroStory.Frontend run lint` — TypeScript type-check (`tsc --noEmit`)
- `npm --prefix src/HeroStory.Frontend run test` — Vitest suite
- `npm --prefix src/HeroStory.Frontend run build` — frontend build

Run the build and the tests for every layer the diff touches, and only those layers. Skip frontend commands for backend-only diffs and vice versa. A build or test failure caused by the diff is a high-severity finding.

For pull request reviews, follow the ordered workflow in `.github/skills/code-review/SKILL.md`, which also covers gathering linked issue context through MCP.

Secrets and external services are not available. Never attempt to call OpenAI, Azure Storage, or a real database; rely on the existing test doubles and the EF InMemory provider used by `tests/HeroStory.IntegrationTests`.

## What to check

Report findings only when you can point at the specific changed line that causes them.

- **Auth and ownership**: every endpoint touching sessions, scenes, or jobs must enforce authentication and verify the resource belongs to the calling user.
- **Development shortcuts**: the dev auth path must stay guarded by both the `Development` environment and `DEV_AUTH_ENABLED=true`, and must issue normal JWT/refresh tokens. Flag anything that weakens this.
- **Secrets**: flag hardcoded credentials, connection strings, or API keys, and any log or error response that could leak them. Non-secret defaults belong in `appsettings.Development.json`; secrets belong in user secrets.
- **Input and errors**: validate input at API boundaries, and surface structured errors rather than raw exception detail to clients.
- **Moderation**: story text paths must keep moderation and safety checks intact.
- **Persistence**: a change to a `HeroStory.Core` entity or `AppDbContext` mapping requires a matching EF Core migration in the same pull request.
- **Version alignment**: EF Core and ASP.NET Core package versions must match across API, Core, Infrastructure, Worker, and test projects.
- **Frontend source hygiene**: `.ts` and `.vue` files under `src/HeroStory.Frontend/src` are canonical. Flag generated `.js` files committed beside source and any change that disables `noEmit`.
- **Story contract**: revisions must preserve prior scene versions rather than overwrite history, and suggestions must remain optional rather than forced.
- **Cross-layer changes**: when a change spans API, worker, and frontend, confirm the request path is consistent end to end (DTO shape, route, and client call all agree).

## What not to comment on

- Formatting or style that the compiler and type-checker already accept.
- Missing docstrings or comments on code the pull request did not change.
- Speculative refactors, added abstractions, or error handling for conditions that cannot occur.
- Stubs and TODOs that are explicitly marked as incomplete work.
