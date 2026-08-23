import type { ArtworkStatus } from "../types/api";

export const isArtworkPending = (status: ArtworkStatus) => status === "queued" || status === "processing";

export const artworkErrorMessage = (code: string | null | undefined) => {
	if (code === "portraitReferenceExpired") return "The portrait request expired. Generate the image again.";
	if (code === "portraitProvenanceMismatch") return "The portrait changed before generation started. Select the current portrait and try again.";
	if (code === "portraitUnavailable") return "The private portrait is no longer available. Upload it again to use likeness artwork.";
	if (code === "portraitConsentMissing") return "Portrait consent was not recorded. Upload the portrait again with consent.";
	return "Artwork unavailable. Generate the image again.";
};