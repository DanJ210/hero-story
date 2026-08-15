import type { ArtworkStatus } from "../types/api";

export const isArtworkPending = (status: ArtworkStatus) => status === "queued" || status === "processing";