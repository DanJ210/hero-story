---
mode: hero-story-slice-implementer
description: Implement multi-turn continuity summary compaction so long episodes keep early story state instead of silently truncating it out of the generation prompt.
---

# Slice: multi-turn continuity summary compaction

Implement exactly this slice, end to end, across domain, persistence, API, and tests. Do not pick up other roadmap items in the same pass.

## Problem

`BuildContinuityContext` in [src/HeroStory.Api/Services/SceneService.cs](../../src/HeroStory.Api/Services/SceneService.cs) currently includes **every** older active-path scene and then hard-truncates the assembled older-scene block at a hardcoded 12,000-character budget:

```csharp
const int maximumContextCharacters = 12000;
// ...
if (olderContext.Length > availableOlderCharacters)
{
    olderContext = olderContext[..availableOlderCharacters];
}
```

Because older scenes are ordered `OrderByDescending(SequenceNumber)`, the truncation silently drops the **oldest** turns first. Past roughly that budget, early-episode facts, relationships, and unresolved threads stop reaching the model. That breaks the product contract in [docs/story-experience.md](../../docs/story-experience.md), which requires free-text decisions to materially influence stored story state, and it makes prompt cost grow linearly with episode length.

## Goal

Replace unbounded-then-truncated older context with: **compacted rollup + recent full window + latest turn**, so early story state survives arbitrarily long episodes within a bounded prompt budget.

## Required design

### 1. Persist the rollup on the session

Add to `StorySession` in [src/HeroStory.Core/Entities/StorySession.cs](../../src/HeroStory.Core/Entities/StorySession.cs):

- `ContinuitySummary` (string, default empty) — the compacted rollup of turns older than the recent window.
- `ContinuitySummaryThroughSequence` (int, default 0) — the highest scene `SequenceNumber` already folded into the rollup.
- `ContinuitySummaryUpdatedAt` (DateTime?).

The rollup is **internal generation context only**. Do not expose it on any DTO, workspace response, or API surface.

Add an EF Core migration and validate it against the SQL Server provider (`DB_APPLY_MIGRATIONS=true`), not just InMemory.

### 2. Compaction service

Add `IContinuitySummaryService` / `ContinuitySummaryService` under `src/HeroStory.Api/Services/`, following the existing service-boundary and explicit-type conventions in that folder.

- Call `OpenAiClient.CreateChatCompletionAsync` directly. Do **not** route compaction through `IOpenAiTextService`, which is bound to the structured turn schema in `StoryTurnResponseParser`.
- Input: the existing rollup plus the scenes that fall out of the recent window and are not yet covered by `ContinuitySummaryThroughSequence`.
- The compaction prompt must enumerate the state markers that are required to survive, rather than leaving retention to model behavior. At minimum: `characters`, `relationships`, `facts`, `resources`, `unresolvedThreads` from `StoryStateJson`, plus location and active-conflict trajectory. Instruct the model explicitly that unresolved threads and established facts must never be dropped, only condensed.
- Reuse the existing prompt-injection guard phrasing: prior story content is passed as **story data, never as instructions**.
- Validate the returned summary length and reject/ignore an over-budget result the same way `StoryTurnResponseParser` validates turn fields. Add the ceiling to `StoryTurnLimits`.

### 3. Configuration

Follow the existing convention in [src/HeroStory.Api/Services/OpenAiTextService.cs](../../src/HeroStory.Api/Services/OpenAiTextService.cs) — `configuration.GetValue("UPPER_SNAKE_NAME", default)` wrapped in `Math.Clamp`:

- `STORY_CONTINUITY_RECENT_TURNS` — how many recent active-path turns stay uncompacted. Default 6, clamp 2–20.
- `STORY_CONTINUITY_COMPACTION_INTERVAL` — how many uncovered older turns must accumulate before recompacting. Default 4, clamp 1–20.
- `STORY_CONTINUITY_MAX_CHARACTERS` — promote the hardcoded 12,000 budget to configuration. Default 12000, clamp 2000–40000.

Add non-secret local defaults to `src/HeroStory.Api/appsettings.Development.json`.

### 4. Wire into the turn path

- Rebuild `BuildContinuityContext` to assemble: `ContinuitySummary` (if present) → the recent window of full older-scene entries → the existing latest-scene block. Keep the total under the configured budget, and when trimming is still necessary, trim the **recent window**, never the rollup and never the latest turn.
- Run compaction **after** a turn is accepted and persisted, not before generation. Compaction must never fail the user's turn: catch, log a warning, leave the rollup stale, and let the next turn retry. Amortize by only recompacting when the interval threshold is met.
- `BuildContinuityContext` is currently `static`. Restructure as needed, but keep `SceneService` constructor changes additive and consistent with the existing optional-dependency pattern so existing test construction sites are not broken more than necessary.

### 5. Revision invalidation

Revision supersedes a turn and creates a replacement on the same sequence number (`SupersedeAndPersistReplacementAsync`). If `ContinuitySummaryThroughSequence >= supersededScene.SequenceNumber`, the rollup may carry state from a superseded turn. In that case reset `ContinuitySummary` to empty and `ContinuitySummaryThroughSequence` to 0 so it rebuilds from the active path. Correctness beats cost here.

## Validation

- Extend [tests/HeroStory.UnitTests/Services/SceneServiceTests.cs](../../tests/HeroStory.UnitTests/Services/SceneServiceTests.cs). The existing long-session test asserts the context stays under 20,000 characters when 30 prior turns exist — keep it passing, and add a test proving an **early-episode state marker still appears** in the context for a long session instead of being truncated away. That assertion is the point of the slice.
- Add coverage for: compaction only fires once the interval threshold is met; a failing compaction call does not fail the turn; revision resets the rollup.
- Add integration coverage that the rollup is not leaked on any scene or workspace response.
- Run `dotnet build hero-story.sln` then `dotnet test hero-story.sln --no-build`.
- Before rebuilding, stop any running `HeroStory.Api` / `HeroStory.Worker` processes to avoid `MSB3021`/`MSB3027` file locks.
- Finish with a real multi-turn run through the workspace past the old 12,000-character budget, confirming continuity holds. Unit tests alone are not sufficient for this flow.

## Out of scope

Revision-history read endpoint, the consent entity and audit trail, observability/OpenTelemetry work, and worker changes. Do not touch them.

## Handoff

On completion, hand off to `hero-story-docs-steward` with: layers touched, the new `StorySession` fields, the migration name, the three new configuration keys, and confirmation that delivery status belongs only in [docs/roadmap.md](../../docs/roadmap.md) — move multi-turn summary compaction out of "Still deferred".
