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

## Proposed decision set (not approved)

These are suggested starting positions to make the open choices concrete. The decisions and their deferred status are tracked in the [roadmap](roadmap.md).

| Topic | Options | Suggested starting position |
| --- | --- | --- |
| Notice and policy version | Approve the existing notice, approve revised wording, or replace it with privacy-owner-approved language. | Review the suggested text above. Keep `hero-likeness-v1` only if the approved scope and material terms remain unchanged; otherwise version the policy and require a fresh grant. |
| Provider scope | Keep a broad provider label, or scope consent to the exact provider/model operation that receives a portrait. | Limit `openai-images` to the configured OpenAI image model and the reference-image generation/edit operation that receives the portrait. Do not extend it to other providers, models, or purposes without a distinct scope and fresh consent. |
| Minor access | Allow minors with appropriate safeguards, prohibit access, or defer the feature until eligibility can be determined. | Do not offer likeness personalization to minors until an age policy and a reliable way to enforce eligibility are approved. Because accounts have no age-band information, eligibility cannot be assumed from the current account data. |
| Provider retention and in-flight requests | Accept verified provider terms, require stronger deletion/retention controls, or withhold portrait submission until terms are acceptable. | Before production use, verify retention and deletion for the exact image operation and account configuration. Explain that app-side deletion cannot recall a request already sent. Accept the in-flight boundary only if provider handling is acceptable; otherwise do not send portraits. |
| Consent/audit record retention and access | Retain for the account lifetime, retain for a defined period after revocation/account deletion, or apply a legally required schedule. | Minimize the recorded data, restrict access to authorized staff, and set a documented retention/deletion schedule based on operational and legal requirements. The exact period needs privacy-owner approval; do not invent one here. |
| Export auditing | Add it before an export feature exists, or add it with that feature. | Defer until a real export operation exists, then audit the operation and its outcome. |

## In-flight provider requests

Revocation prevents queued work and work that has not been dispatched from using the portrait. A provider request already in flight cannot be recalled; if the grant or portrait is revoked before the result is attached, the application discards that result.
