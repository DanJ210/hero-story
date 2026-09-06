---
name: code-review
description: "Review Hero Story pull requests and local changes. Use when reviewing a pull request, reviewing a diff, or checking changed code before merge. Prepares context by fetching linked issues through the GitHub MCP server, runs the build, type-check, and tests for the layers the diff touches, then reports findings against the Hero Story auth, moderation, persistence, and cross-layer checks."
---

# Hero Story code review

Follow `.github/instructions/code-review.instructions.md` for the full check list and the "what not to comment on" rules. This skill defines the order of work and the external context to gather first.

## 1. Gather context

Use the GitHub MCP server before reading the diff:

- Read the pull request body and any issue it references (`Fixes #123`, `Closes #123`) to learn the intended behavior. Review the diff against that intent, not against a guess.
- Read the pull request's existing review comments so you do not repeat a point another reviewer already made.

If there is no pull request (reviewing local changes or an unpushed branch), skip the MCP lookups, infer intent from commit messages and the diff itself, state that assumption at the top of the report, and continue with steps 2–4.

If the pull request description names an external item you cannot resolve through a configured MCP server, say so once rather than inventing the missing context. If the GitHub MCP server itself is unavailable or returns an error, state this once at the top of the report, review the diff on its own merits, and do not assume any referenced issue's intent.

## 2. Identify the touched layers

Map changed paths to layers, and carry that mapping into step 3:

| Changed paths | Layer |
|---|---|
| `src/HeroStory.Api/**`, `src/HeroStory.Core/**`, `src/HeroStory.Infrastructure/**`, `src/HeroStory.Worker/**`, `tests/**` | backend |
| `src/HeroStory.Frontend/**` | frontend |

Paths not listed in the table map to no layer. If a diff touches only unmapped paths, skip step 3 and note in the report that no validation commands were run. If an unmapped path is a solution/build/config file (`*.sln`, `*.props`, `*.csproj`, `package.json`, `package-lock.json`), treat it as the layer it configures and run that layer's commands.

## 3. Validate

The environment is already prepared by `.github/workflows/copilot-code-review.yml`. Run only the commands for the touched layers.

Backend:

```bash
dotnet build hero-story.sln
dotnet test hero-story.sln --no-build
```

Frontend:

```bash
npm --prefix src/HeroStory.Frontend run lint
npm --prefix src/HeroStory.Frontend run test
```

For a diff that touches both layers, run both sets. A build or test failure caused by the diff is a high-severity finding; quote the failing output in the comment.

If a failure is in a file or test the diff does not touch, check whether it also fails on the base branch (e.g. `git stash` / checkout base and rerun). Report pre-existing failures once as a low-severity note labelled "pre-existing, not caused by this diff" and do not block on them. If you cannot determine the cause, report it as medium severity and state that the cause is unconfirmed.

Secrets and external services are unavailable. Do not call OpenAI, Azure Storage, or a real database.

## 4. Report

- Attach each finding to the specific changed line that causes it.
- Assign severity: high for build/test failures caused by the diff (see step 3 for pre-existing or unconfirmed failures), auth or ownership gaps, leaked secrets, and missing migrations; medium for cross-layer contract mismatches and version drift; low for everything else.
- State the fix concretely enough to apply, and prefer a suggested change when the fix is a small edit.
- If the diff is clean, say so rather than manufacturing comments.
