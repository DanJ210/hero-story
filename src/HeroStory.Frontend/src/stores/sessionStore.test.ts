import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as sessionApi from "../api/sessionApi";
import type { CreateStorySessionResponse } from "../types/api";
import { useSessionStore } from "./sessionStore";

vi.mock("../api/sessionApi");

describe("sessionStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.resetAllMocks();
  });

  it("returns the generated opening scene when creating a story", async () => {
    const result = createStoryResult();
    vi.mocked(sessionApi.createSession).mockResolvedValue(result);
    vi.mocked(sessionApi.getSessions).mockResolvedValue([]);
    const store = useSessionStore();

    const created = await store.createSession({ title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari" });

    expect(created.openingScene.storyBeat).toBe("opening");
    expect(store.currentSession).toEqual(result.session);
  });
});

const createStoryResult = (): CreateStorySessionResponse => ({
  session: {
    id: "session-1",
    title: "Origin",
    genre: "Superhero",
    heroArchetype: "Guardian",
    heroName: "Ari",
    status: "active",
    moderationFailureCount: 0,
    createdAt: "2026-08-14T00:00:00Z",
    updatedAt: "2026-08-14T00:00:00Z",
    likenessEnabled: false
  },
  openingScene: {
    id: "scene-1",
    sessionId: "session-1",
    sequenceNumber: 1,
    choiceText: "The story begins.",
    narrativeText: "Opening narrative",
    sceneSummary: "The adventure begins.",
    location: "City center",
    activeConflict: "A threat emerges",
    storyStateSchemaVersion: 1,
    storyState: {},
    suggestedActions: ["Investigate", "Protect civilians"],
    storyBeat: "opening",
    isEpisodeComplete: false,
    artworkStatus: "queued",
    artworkErrorCode: null,
    imageUrl: null,
    imageUrlExpiresAt: null,
    moderationStatus: "approved",
    moderationDetail: null,
    createdAt: "2026-08-14T00:00:00Z",
    updatedAt: "2026-08-14T00:00:00Z"
  }
});