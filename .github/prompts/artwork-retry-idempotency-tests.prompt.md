---
agent: hero-story-slice-implementer
description: "Vertical-slice tests proving automatic artwork retry and completed-job idempotency."
---

Implement the Near-term MVP completion item from `docs/roadmap.md`:

> Add vertical-slice tests proving automatic artwork retry and completed-job idempotency.

## Scope boundary

This is a test-coverage and validation slice. Do not change core artwork or queue processing logic unless a test proves a genuine gap in retry or idempotency behavior.

Layers in play: `tests/HeroStory.IntegrationTests` and `tests/HeroStory.UnitTests/Worker`, plus any minimal test seams required in `src/HeroStory.Worker` or `src/HeroStory.Infrastructure`.

## Current state (verified)

- `ImageGenerationWorker.ProcessMessageAsync` handles queue messages for image jobs.
- When `job.Status` is `Completed` or `Poisoned`, the worker logs information and immediately deletes the message from the queue without re-running generation (terminal job idempotency).
- When generation fails and `message.DequeueCount < MaxDequeueCount`, the job status becomes `Failed`, allowing redelivery.
- When `message.DequeueCount >= MaxDequeueCount`, the job status becomes `Poisoned` and the message is moved to the poison queue.
- `AzureQueueClient` and `ImageGenerationWorker` support test doubles / internal message processing.

## Gaps to close

Add vertical-slice / integration-level test coverage proving:

1. **Completed-Job Idempotency**:
   - Send or simulate a redelivered queue payload for a job that is already in `JobStatus.Completed` state.
   - Assert that image generation (OpenAI call / image upload) is NOT executed again, the job remains `Completed`, and the message is safely acknowledged/deleted.

2. **Poisoned-Job Idempotency**:
   - Send or simulate a redelivered queue payload for a job in `JobStatus.Poisoned` state.
   - Assert generation is skipped and the message is deleted from the active queue.

3. **Automatic Artwork Retry**:
   - Simulate a transient failure during artwork generation on attempt 1 (`DequeueCount = 1`).
   - Assert the job status transitions to `Failed`, attempt count is updated, and the message is not moved to poison.
   - Simulate subsequent redelivery until `DequeueCount >= MaxDequeueCount`.
   - Assert the job status transitions to `Poisoned` and the message is moved to poison queue.

4. **Successful Retry Recovery**:
   - Simulate a failed first attempt (`Failed`), followed by a successful redelivered attempt (`DequeueCount = 2`).
   - Assert the job successfully completes (`JobStatus.Completed`), image URL is assigned to the scene, and message is deleted.

## Approach constraints

- Reuse existing test fixtures and helpers in `tests/HeroStory.IntegrationTests` or `tests/HeroStory.UnitTests/Worker`.
- Use fakes/mocks for external network dependencies (`OpenAiClient`, `AzureBlobService`, `AzureQueueClient`). No real Azure or OpenAI calls.
- Every test must assert clean state transitions and verify that external side effects (like image generation) happen strictly when expected and are skipped on idempotent redeliveries.

## Validation

```bash
dotnet build hero-story.sln
dotnet test hero-story.sln --no-build
```

Ensure no active `HeroStory.Api` or `HeroStory.Worker` processes are holding file locks during the build.

## Done means

All retry and idempotency scenarios pass with verified assertions, the full solution builds and tests green, and `docs/roadmap.md` is updated to mark the near-term retry/idempotency test item complete.
