import { describe, expect, it } from "vitest";
import type { ArtworkStatus } from "../types/api";
import { isArtworkPending } from "./artworkStatus";

describe("isArtworkPending", () => {
  it.each<ArtworkStatus>(["queued", "processing"])("returns true for %s", (status) => {
    expect(isArtworkPending(status)).toBe(true);
  });

  it.each<ArtworkStatus>(["notRequested", "completed", "failed", "poisoned"])("returns false for %s", (status) => {
    expect(isArtworkPending(status)).toBe(false);
  });
});