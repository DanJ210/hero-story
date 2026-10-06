# Hero-likeness consent policy review

This review records the consent choices represented by the application and the product/privacy decisions that still need approval. It is a review draft, not legal advice or a substitute for an approved privacy notice.

## Consent values represented in the application

| Field | Value |
| --- | --- |
| Purpose | `story-artwork-likeness` |
| Policy version | `hero-likeness-v1` |
| Provider scope | `openai-images` |

The grant is tied to the exact portrait version and remains immutable. Replacing, disabling, or deleting that portrait revokes the grant through an append-only event. Automatic session likeness and individual scene likeness remain separately opt-in. The worker validates the grant, owner, provider scope, active portrait version, and revocation state before provider use; it checks again before attaching generated artwork.

Portrait rows created under timestamp-only consent have no versioned grant and cannot authorize generation. Their owners can replace the portrait and make a new explicit grant.

## Consent notice text for review

The upload control presents this text:

> I own or am authorized to use this image. I consent to private storage and to OpenAI image generation using it only as a reference for my story artwork.

The page also states that generated artwork remains part of the story after portrait removal.

### Suggested replacement for review

> I confirm that I own or am authorized to use this image and have permission to use the likeness of each identifiable person shown for this purpose. I consent to Hero Story storing this portrait privately and sending it to OpenAI's image service when I request story artwork using my likeness. The portrait will be used only as a reference for that artwork. Removing the portrait stops future use but does not remove artwork already generated. A request already sent to OpenAI cannot be recalled. OpenAI's handling and retention of submitted images is subject to the applicable service terms and data controls ([link to be confirmed]).

This is draft language, not approved notice copy. Confirm the rights/permission statement and the provider wording with the privacy owner, verify the provider terms and data controls for the configured operation, and add the correct link before presenting the text as a policy. If the approved consent changes the purpose, provider, scope, or material retention terms, issue a new policy version and obtain a new grant. Keep `hero-likeness-v1` only if the approved terms remain materially the same.

### Hero-preview consent draft

The product owner approved this draft in principle for privacy-owner review. It is not approved notice copy and does not authorize preview generation:

> I confirm that I own or am authorized to use this portrait. I consent to Hero Story storing it privately and sending it to OpenAI's Images API when I request a hero preview. OpenAI states that API inputs are not used to train or improve its models unless the customer opts in. By default, image-edit requests may be included in abuse-monitoring logs for up to 30 days, and may be retained longer when required by law or reasonably necessary to protect OpenAI, its services, or others. OpenAI scans image inputs for prohibited content; images flagged for potential child sexual abuse material may be retained for manual review, even when data-retention controls apply. Disabling, replacing, or deleting this portrait deletes it and its private hero previews from Hero Story, but cannot recall a request already sent to OpenAI or delete any provider-retained copy. This consent is only for generating a private hero preview. Story artwork requires separate consent and an explicit story or scene opt-in. [OpenAI data controls](https://developers.openai.com/api/docs/guides/your-data).

OpenAI's documentation states that `/v1/images/edits` has no application-state retention, is eligible for Zero Data Retention subject to limitations, and has up to 30 days of default abuse-monitoring retention. Zero Data Retention and Modified Abuse Monitoring require OpenAI approval and account/project configuration; the repository does not establish the effective setting for the evaluation or deployment account. The image-input scanning exception above applies even with those controls. The documentation also states API inputs are not used for model training unless the customer opts in. Verify the actual account settings and have the privacy owner approve final wording and the linked terms before collecting a preview grant.

The worker development profile selects `gpt-image-1`. OpenAI's [Image API documentation](https://developers.openai.com/api/reference/resources/images) describes image edits from one or more source images, accepts up to 16 image inputs for GPT Image models, and documents optional `input_fidelity` values `high` and `low` for `gpt-image-1`. The existing application request uses one source image and does not set `input_fidelity`. This verifies documented API capability only; it does not establish account authorization, identity-preservation quality, or the quality of multiple references used together.

## Remaining decisions for privacy-owner review

Product choices for the preview flow are recorded in [story-experience.md](story-experience.md#hero-preview-and-approval). Privacy, provider-account, and release gates remain in the [roadmap](roadmap.md).

## In-flight provider requests

Revocation prevents queued work and work that has not been dispatched from using the portrait. A provider request already in flight cannot be recalled; if the grant or portrait is revoked before the result is attached, the application discards that result.
