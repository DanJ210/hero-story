# Interactive story experience

## Product promise

Hero Story is a serialized superhero story in which the reader is the main character. It uses a conversational interaction loop, but the primary output is book-like narrative rather than a general-purpose chat transcript.

The user defines a hero, reads the next passage, and responds with what their hero says, attempts, or chooses. The system advances the story while preserving continuity and making the user's decisions materially affect later events.

## Experience principles

1. **The user is the hero.** Narrative addresses the configured hero as the protagonist and does not replace them with an autonomous lead character.
2. **Story first, chat second.** Generated passages read as polished prose. The conversation model exists to make participation easy.
3. **Choices have consequences.** User actions can alter relationships, resources, risks, locations, conflicts, and the ending of an episode.
4. **The user owns the direction.** Free-text actions are always accepted when safe; suggestions are optional prompts, not restrictions.
5. **Revision is supported.** The user can revise a turn and continue from the revised version without silently rewriting the historical record.
6. **Continuity is explicit.** The system stores compact structured state instead of relying only on replaying an ever-growing transcript.
7. **Safety stays in the loop.** User input and generated output remain moderated before they become part of the active story.

## MVP interaction loop

1. The user creates a story session with a hero name, archetype or powers, genre, tone, and optional premise.
2. The system generates an opening passage or continues from the active story state.
3. The passage is approximately 250–500 words and ends with a situation that invites action.
4. The system offers 2–3 suggested actions.
5. The user either selects a suggestion or enters a free-text action.
6. The input is moderated and interpreted as intent, dialogue, or action.
7. The system generates the next passage and a structured state update.
8. Selected major story beats enqueue artwork; ordinary turns do not require an image.
9. The loop continues until the episode reaches a deliberate conclusion or the user pauses it.

## MVP product defaults

- One user controls one hero in a session.
- One episode is active at a time and normally targets 8–15 turns, while allowing earlier or later conclusions when pacing requires it.
- The hero can fail an attempt, suffer setbacks, lose resources, or damage relationships. The story should convert failure into consequence and a new decision rather than ending participation unexpectedly.
- Permanent hero death is not introduced by surprise in the MVP. Irreversible outcomes require a future explicit user preference and safety design.
- The default tone is suitable for a broad teen audience. More specific content-rating controls remain a product decision before production launch.
- Free text is authoritative; suggested actions never limit what the user may safely attempt.

## Turn contract

A story turn combines the user's contribution, generated prose, and the state needed for the next turn.

The target generation result is conceptually:

```json
{
  "narrative": "The bridge trembles as the reactor wakes beneath your feet...",
  "sceneSummary": "The hero reached the reactor chamber and learned the engineer knows their identity.",
  "location": "Skybridge reactor chamber",
  "activeConflict": "Stop the reactor before the bridge collapses",
  "storyState": {
    "characters": [],
    "relationships": [],
    "facts": [],
    "resources": [],
    "unresolvedThreads": []
  },
  "suggestedActions": [
    "Disable the reactor",
    "Confront the engineer",
    "Rescue the trapped workers"
  ],
  "storyBeat": "major",
  "isEpisodeComplete": false
}
```

The model response must be parsed and validated as structured data. Invalid output should fail safely or be retried; application state must not be derived by brittle string parsing.

## Continuity and influence

Each generation request should include:

- stable hero and session configuration,
- the current compact story state,
- summaries of relevant prior turns,
- the most recent narrative passage,
- the new user contribution,
- pacing, length, safety, and output-schema instructions.

A turn should record which state changed because of the user's action. The narrative must acknowledge that action directly and produce at least one observable consequence unless the action is impossible within established story rules. When an action cannot succeed, the story should explain why and still allow the attempt to affect the situation.

## Revision model

Story turns are treated as immutable versions. Revising a turn creates a replacement branch from the preceding accepted turn rather than overwriting generated history in place.

For the MVP:

- the user can revise the latest active turn,
- the prior version remains stored but is marked superseded,
- any artwork or generation job tied only to the superseded version is no longer part of the active story path,
- the revised user contribution is moderated and regenerated,
- session reads return the active path by default.

The data model should preserve parent/revision relationships so revision of older turns and explicit branch exploration can be added later without redesigning the core history model.

## Episodes and completion

A story session contains one active episode for the MVP. An episode should establish a conflict, escalate it, reach a climax, and conclude based on accumulated choices. Completion is explicit in structured state rather than inferred from prose.

The user may:

- pause and resume an active episode,
- request a conclusion when ready,
- revise the latest turn before continuing,
- start another episode with the same hero after completion.

Multi-episode campaigns and multiple simultaneously active branches are post-MVP capabilities.

## Deferred product decisions

Before production launch, define explicit policy for age bands, content ratings, romance, irreversible character death, and whether users can publish or share stories. These decisions affect moderation, prompting, data retention, and UX and must not be left solely to model behavior.

## Artwork policy

Artwork is selective to control latency, cost, and visual repetition. Generate it for:

- the opening scene,
- a major reveal or location change,
- a climax,
- an episode conclusion,
- an explicit user request when supported.

The structured `storyBeat` value drives this decision. Image generation remains asynchronous and must not block the narrative response.

## UX direction

- Present generated prose with readable book-like typography and spacing.
- Present user contributions as compact actions between passages.
- Keep the input anchored to the current story with language such as “What does your hero do?”
- Show 2–3 suggested actions near the input while preserving free-text entry.
- Provide a visible revision action on the latest turn.
- Keep the active story path readable as a continuous episode.
- Treat image status as secondary to reading and decision-making.

## Current implementation gap

The current implementation validates a structured JSON model response and persists narrative, summary, location, active conflict, schema-versioned story state, 2–3 suggested actions, story-beat classification, and episode-completion status per `Scene`. Each subsequent turn receives the latest accepted scene summary, location, conflict, state, and narrative passage as bounded continuity context.

The parser enforces the 250–500 word target, field lengths, 2–3 distinct suggestions, object-shaped state, and a 16 KB serialized state limit. Persisted context must use supported schema version 1.

It does not yet build summaries across multiple older turns, retry malformed provider responses, implement revision lineage or active-path reads, update session episode status, or apply selective image dispatch. It still creates one image job for every scene.

These gaps are implementation work, not completed behavior. Delivery sequencing is tracked in [roadmap.md](roadmap.md).

## MVP acceptance criteria

The interactive story vertical slice is complete when:

1. An authenticated user can create a hero and begin an episode.
2. Each turn accepts free text and displays 2–3 optional suggestions.
3. Generated passages are normally 250–500 words and read as continuous prose.
4. A user's action is acknowledged and changes stored story state or produces an explained consequence.
5. Continuity survives at least one complete episode without replaying the full raw transcript on every request.
6. The latest turn can be revised, with the prior version retained and removed from the active path.
7. Episode completion is explicit and the completed active path remains readable.
8. Artwork is queued only for selected story beats and does not block narrative generation.
9. Moderation, ownership checks, and normal authenticated access apply to creation, continuation, revision, and reads.
