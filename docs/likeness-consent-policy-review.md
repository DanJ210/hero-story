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

The page also states that generated artwork remains part of the story after portrait removal. Please confirm that the notice clearly explains the purpose, provider, and output-retention behavior, and supply any required wording or links to a privacy notice.

## Decisions required

1. Approve or replace the notice wording and the `hero-likeness-v1` policy-version identifier.
2. Confirm whether `openai-images` is sufficiently narrow and which image API/model operations it covers. A different provider requires a distinct scope and a new user grant.
3. Decide whether minors may use hero-likeness personalization. The product documentation leaves this decision open, and the account model has no age-band information.
4. Confirm provider-side portrait retention and deletion expectations. Application deletion removes private portrait blobs, but it cannot recall data already sent in a provider request.
5. Define retention and access rules for the consent/audit records.
6. Revisit export auditing when a real export operation exists. No export endpoint or event is present.

## In-flight provider requests

Revocation prevents queued work and work not yet dispatched from using the portrait. A provider request already in flight cannot be recalled; if the grant or portrait is revoked before the result is attached, the application discards that result. Confirm that this boundary matches the intended provider-retention policy.
