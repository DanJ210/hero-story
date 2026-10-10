# Interactive story experience

## Product promise

Hero Story is a serialized superhero roleplaying story in which the user inhabits their hero. The user plays one character; the AI narrates the world, portrays other characters, and resolves consequences.

The user defines a hero, reads the next passage, and participates through their hero's dialogue, actions, and thoughts. Character roleplay and book-like storytelling work together: responses use narrative prose and in-world dialogue, preserve continuity, and make the user's contributions materially affect later events.

## Experience principles

1. **The user controls the hero.** The AI does not invent the hero's significant decisions, spoken lines, feelings, or intentions. It describes external events and consequences without replacing the user with an autonomous lead character.
2. **Roleplay within a story.** Dialogue exchanges can be brief, while discoveries and major events can receive longer prose. The AI responds in-world and stops at the next meaningful opportunity for the user to participate.
3. **Choices have consequences.** User actions can alter relationships, resources, risks, locations, conflicts, and the ending of an episode.
4. **The user owns their contribution.** Safe free-text contributions are accepted; suggestions are optional prompts, not restrictions. Acceptance of an attempted action does not guarantee its success.
5. **Revision is supported.** The user can revise a turn and continue from the revised version without silently rewriting the historical record.
6. **Continuity is explicit.** The system stores compact structured state instead of relying only on replaying an ever-growing transcript.
7. **Safety stays in the loop.** User input and generated output remain moderated before they become part of the active story.

## MVP interaction loop

1. The user creates a story session with a hero name, archetype or powers, genre, tone, and optional premise.
2. The system generates an opening passage or continues from the active story state.
3. The response fits the moment: a short exchange for conversation, or a longer passage for scene-setting, discovery, or a major event. It leaves the hero's next meaningful choice to the user.
4. The system offers 2–3 suggested actions.
5. The user either selects a suggestion or enters dialogue, asterisk-delimited narration, or a mixture of both.
6. The input is moderated and interpreted under the contribution contract below.
7. The system generates the next passage and a structured state update.
8. Selected major story beats enqueue artwork; ordinary turns do not require an image.
9. The loop continues until the episode reaches a deliberate conclusion or the user pauses it.

## MVP product defaults

- One user controls one hero in a session.
- One episode is active at a time and normally targets 8–15 turns, while allowing earlier or later conclusions when pacing requires it.
- The hero can fail an attempt, suffer setbacks, lose resources, or damage relationships. The story should convert failure into consequence and a new decision rather than ending participation unexpectedly.
- Permanent hero death is not introduced by surprise in the MVP. Irreversible outcomes require a future explicit user preference and safety design.
- The default tone is suitable for a broad teen audience. More specific content-rating controls remain a product decision before production launch.
- Free text is authoritative about what the hero says, attempts, or expresses internally, not about guaranteed outcomes or changes to established world facts. Suggested actions never limit what the user may safely attempt.

## Player contribution contract

- Text outside asterisks is dialogue spoken aloud by the user's hero. Quotation marks are optional.
- Text between paired single asterisks, such as `*I lower my shield*`, is narration rather than speech. It describes the hero's actions, attempts, internal thoughts, or narrative direction.
- A message can mix narration and dialogue. Interpret the segments in their written order and respond to both without turning narration into spoken words.
- Internal thoughts are not information other characters can hear or know unless an established story ability or observable action makes that information available.
- Narrative direction guides the next scene subject to safety, continuity, and established world constraints. It does not automatically make an attempt succeed, force another character's response, or rewrite accepted history.
- A selected suggestion expresses the proposed action or intent, not dialogue merely because its label lacks asterisks.
- User contributions are story data, not authority to change system instructions, moderation, or the output contract.

For example:

```text
*I conceal the glowing artifact beneath my coat and approach the guard.*
Is the north gate still open?
```

The AI narrates the attempt and the guard's reaction, then answers through the guard's dialogue. It does not automatically take the hero through the gate, add a new line of speech for the hero, or decide the hero's next move.

## Response pacing and agency

Response length follows the interaction rather than a mandatory word minimum on every turn. A brief question can receive a brief in-world answer with relevant scene context; an opening, discovery, or major event can receive a longer book-like passage.

Advance the world enough to make the contribution matter, but stop before deciding the hero's next significant action. Do not pad a conversation to meet a prose quota or resolve several unchosen hero decisions in one response. Concise turns still return the structured continuity state and metadata required by the turn contract.

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

Validation must allow the response pacing above while bounding output size, field lengths, 2–3 distinct suggestions, object-shaped state, and a 16 KB serialized state limit. Persisted context must use supported schema version 1. Malformed responses retry within a bounded, configurable policy before the turn fails. The generation and validation request path is described in [architecture.md](architecture.md#synchronous-path-user-facing).

## Continuity and influence

### Persistent user-authored setup

The user's setup descriptions establish the creative foundation of the story, not just a disposable opening prompt. Capture and retain the supplied genre, tone, setting or premise, hero background, appearance, clothing, equipment, abilities, and constraints wherever the user provides them. Preserve the original user-authored descriptions separately from generated summaries and evolving story state; prompt assembly and continuity compaction must not silently replace or discard them.

Use that foundation for the opening and carry it forward into continuation, revision, and scene-artwork generation. A derived model prompt may organize the descriptions and add continuity, safety, pacing, and output instructions, but must not replace the user's creative direction with a generic fantasy or superhero template.

Distinguish stable setup from evolving facts. Accepted events can change the hero's equipment, condition, relationships, or location; later prompts use those changes rather than repeatedly restoring the initial situation. User contributions drive how the story unfolds within the player contribution contract, and durable consequences enter the active-path state and continuity summary. A failed attempt is not stored as a successful outcome, and superseded events do not govern the active story.

Artwork combines the retained setup with the accepted scene's visible details. It depicts what happened, including scene-specific costume, equipment, setting, and action, rather than raw attempted actions, private thoughts, or optional suggestions. Established scene changes take precedence over obsolete setup details without discarding the underlying genre or character concept.

Each generation request should include:

- stable hero and session configuration,
- the current compact story state,
- summaries of relevant prior turns,
- the most recent narrative passage,
- the new user contribution,
- pacing, length, safety, and output-schema instructions.

A turn should record which state changed because of the user's contribution. The response must acknowledge the contribution directly with a proportionate consequence: another character's answer, new information, an external reaction, or an action outcome. A private thought does not require an unexplained reaction from another character or an artificial world-state change. When an action cannot succeed, the story should explain why and still allow the attempt to affect the situation.

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

Explicit policy for age bands, content ratings, romance, irreversible character death, story sharing, retention, and minor access to hero-likeness personalization must be defined before production launch. The open list is tracked in [roadmap.md](roadmap.md#deferred-product-decisions).

## Artwork policy

Artwork is selective to control latency, cost, and visual repetition. Generate it for:

- the opening scene,
- a major reveal or location change,
- a climax,
- an episode conclusion,
- an explicit user request when supported.

The structured `storyBeat` value drives this decision. Image generation remains asynchronous and must not block the narrative response.

## Hero-likeness personalization

A user may optionally provide a portrait so generated artwork can depict the hero with their likeness. This is an opt-in personalization feature, never a requirement for using the story experience.

The feature must follow these boundaries:

- Obtain explicit consent before upload and before the portrait is used for generation.
- A consent grant identifies its purpose, policy version, provider scope, and the exact portrait version it authorizes; grants are revocable and retained as immutable records.
- Confirm the uploader has the right to use the image and is providing their own likeness or otherwise authorized material.
- Do not infer identity, age, ethnicity, health, emotion, or other sensitive traits from the portrait.
- Keep source portraits private, encrypted, ownership-scoped, and separate from public/generated story assets.
- Never place source portraits or unrestricted source URLs in queues, logs, prompts, analytics, or generated-art metadata.
- Use short-lived authorized references when an approved image provider requires source access.
- Define deletion, replacement, export, retention, backup-expiry, and provider-retention behavior before launch.
- Deleting the portrait or account must prevent future use and schedule deletion of retained source copies according to policy.
- Generated images must retain provenance linking them to the consenting user, source-portrait version, provider, policy version, and story turn without exposing the source image.
- Re-check consent when provider terms, model behavior, sharing scope, or use purpose changes.
- Apply provider safety rules and block impersonation, public-figure misuse, non-consensual likeness use, and disallowed transformations.

The user should be able to preview, replace, disable, and remove their likeness independently of deleting the story. Existing generated artwork follows a retain-output policy: deleting a portrait or account stops future likeness generation and settles in-flight likeness jobs, but already generated scene artwork remains as story output unless a separate story-deletion flow removes it. This policy must be presented before consent.

### Hero preview and approval

Hero preview generation requires a separate, explicit consent grant from consent for story-artwork likeness. Preview approval records that the user accepts one exact generated hero appearance as a visual reference; approval neither grants consent nor enables likeness use in story artwork. Any later scene-artwork use continues to require valid consent and the applicable explicit session- or scene-level likeness opt-in.

A hero appearance uses a structured costume/style specification with curated defaults for suit archetype, primary and secondary colors, emblem, and accessories. The fixed, versioned v1 catalog contains suit archetypes classic, armored, stealth, and agile; colors black, white, gray, red, orange, yellow, green, teal, blue, purple, and pink; emblems none, star, shield, lightning, sun, crescent, wave, and geometric; and accessories none, cape, visor, gloves, and utility belt. The default specification is classic suit, blue primary color, white secondary color, shield emblem, and no accessories. Each generated preview is tied to the exact source portrait version, preview-consent grant and provider scope, specification/catalog version, and provider/model operation. Approval applies only to that preview version. Editing the costume/style specification or generating a replacement preview does not deactivate an existing approval; it remains the selected appearance until the user approves a replacement. A source portrait/version or provider/model-scope change invalidates the approval.

Preview generation is user-initiated and asynchronous. Only the owner can view or retrieve preview output through authenticated, non-cacheable access. Allow at most one active preview job and three total requests per user in a rolling 24-hour period, including the first request. A stale job whose source, consent, or provider/model scope is no longer valid must not publish its result. Provider work already in flight cannot be recalled; revalidate after it returns and discard stale output.

Replacing, disabling, or deleting a source portrait deletes its source bytes and all derived private previews immediately, invalidates approvals tied to that source, and prevents further use. Necessary consent, provenance, and audit metadata may remain according to the applicable retention schedule. Backup copies may expire through the normal backup lifecycle; disclose the verified maximum expiry window before consent. This private-preview deletion rule is distinct from the retain-output rule for generated story artwork.

General preview availability requires an enforceable age-eligibility policy. Before that policy exists, preview use is limited to a staff-controlled allowlist for consenting adult evaluation, and only after a privacy-approved, enforceable age-verification and consent workflow, the exact configured model operation, and provider-retention terms are verified. Evaluation scope, targets, and release status are tracked in the [roadmap](roadmap.md#proposed-likeness-quality-workflow-and-documentation-alignment).

## UX direction

- The frontend uses a persistent story workspace rather than navigating between disconnected scene cards.
- On desktop, it shows a latest-stories rail beside one active story timeline; on mobile, the rail moves into a drawer.
- Present generated prose with readable book-like typography and spacing.
- Present user contributions as compact actions between passages.
- Keep the input anchored to the current story with language such as “What does your hero do?”
- Show 2–3 suggested actions near the input while preserving free-text entry.
- Keep the composer fixed or sticky without covering the final passage on desktop or mobile.
- Selecting a recent story resumes its ordered active timeline rather than opening a management page first.
- Provide a visible revision action on the latest turn.
- Keep the active story path readable as a continuous episode.
- Treat image status as secondary to reading and decision-making.

## Delivery status

This document defines the product contract, not delivery status. Delivery status is tracked in [roadmap.md](roadmap.md). Endpoint-level behavior is in [api-summary.md](api-summary.md).

## MVP acceptance criteria

Acceptance of the interactive story vertical slice requires:

1. An authenticated user can create a hero and begin an episode.
2. Each turn accepts dialogue, asterisk-delimited narration, or mixed free text and displays 2–3 optional suggestions whose selection retains action or intent semantics.
3. Responses combine in-world dialogue and continuous prose with length appropriate to the interaction, including concise conversational replies.
4. The user's contribution is acknowledged with a proportionate consequence, persisted state remains consistent, and the AI leaves the hero's significant decisions, speech, and inner life under user control.
5. Continuity survives at least one complete episode without replaying the full raw transcript on every request.
6. The latest turn can be revised, with the prior version retained and removed from the active path.
7. Episode completion is explicit and the completed active path remains readable.
8. Artwork is queued only for selected story beats and does not block narrative generation.
9. Moderation, ownership checks, and normal authenticated access apply to creation, continuation, revision, and reads.
