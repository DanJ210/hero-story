import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as sceneApi from "../api/sceneApi";
import * as sessionApi from "../api/sessionApi";
import type { SceneDto, StoryWorkspaceDto } from "../types/api";
import { useWorkspaceStore } from "./workspaceStore";

vi.mock("../api/sceneApi");
vi.mock("../api/sessionApi");

describe("workspaceStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.resetAllMocks();
  });

  it("loads the aggregate workspace and appends a continuation", async () => {
    const opening = createTurn("scene-1", 1, "opening");
    const continuation = createTurn("scene-2", 2, "standard");
    const workspace: StoryWorkspaceDto = {
      session: { id: "session-1", title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari", status: "active", moderationFailureCount: 0, createdAt: "2026-08-15T00:00:00Z", updatedAt: "2026-08-15T00:00:00Z" },
      turns: [opening]
    };
    vi.mocked(sessionApi.getWorkspace).mockResolvedValue(workspace);
    vi.mocked(sceneApi.createScene).mockResolvedValue(continuation);
    const store = useWorkspaceStore();

    await store.load("session-1");
    await store.continueStory("session-1", "Protect the city");

    expect(store.workspace?.turns.map((turn) => turn.id)).toEqual(["scene-1", "scene-2"]);
    expect(store.latestTurn?.storyBeat).toBe("standard");
  });

  it("refreshes artwork state without entering the blocking load state", async () => {
    const workspace: StoryWorkspaceDto = {
      session: { id: "session-1", title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari", status: "active", moderationFailureCount: 0, createdAt: "2026-08-15T00:00:00Z", updatedAt: "2026-08-15T00:00:00Z" },
      turns: [createTurn("scene-1", 1, "opening")]
    };
    vi.mocked(sessionApi.getWorkspace).mockResolvedValue(workspace);
    const store = useWorkspaceStore();

    const refresh = store.refresh("session-1");

    expect(store.loading).toBe(false);
    await refresh;
    expect(store.workspace).toEqual(workspace);
  });

  it("revises the latest turn and refreshes the active workspace path", async () => {
    const opening = createTurn("scene-1", 1, "opening");
    const originalLatest = createTurn("scene-2", 2, "standard");
    const replacement = createTurn("scene-3", 2, "standard");
    const initialWorkspace: StoryWorkspaceDto = {
      session: { id: "session-1", title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari", status: "active", moderationFailureCount: 0, createdAt: "2026-08-15T00:00:00Z", updatedAt: "2026-08-15T00:00:00Z" },
      turns: [opening, originalLatest]
    };
    const refreshedWorkspace: StoryWorkspaceDto = { ...initialWorkspace, turns: [opening, replacement] };
    vi.mocked(sessionApi.getWorkspace).mockResolvedValueOnce(initialWorkspace).mockResolvedValueOnce(refreshedWorkspace);
    vi.mocked(sceneApi.reviseScene).mockResolvedValue(replacement);
    const store = useWorkspaceStore();

    await store.load("session-1");
    const result = await store.reviseLatestTurn("session-1", originalLatest.id, "Protect the bridge");

    expect(result).toEqual(replacement);
    expect(sceneApi.reviseScene).toHaveBeenCalledWith("session-1", originalLatest.id, "Protect the bridge");
    expect(store.workspace?.turns.map((turn) => turn.id)).toEqual(["scene-1", "scene-3"]);
    expect(store.latestTurn?.id).toBe("scene-3");
  });

  it("pauses and resumes the episode while refreshing workspace status", async () => {
    const activeWorkspace: StoryWorkspaceDto = {
      session: { id: "session-1", title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari", status: "active", moderationFailureCount: 0, createdAt: "2026-08-15T00:00:00Z", updatedAt: "2026-08-15T00:00:00Z" },
      turns: [createTurn("scene-1", 1, "opening")]
    };
    const pausedWorkspace: StoryWorkspaceDto = { ...activeWorkspace, session: { ...activeWorkspace.session, status: "paused" } };
    vi.mocked(sessionApi.getWorkspace).mockResolvedValueOnce(activeWorkspace).mockResolvedValueOnce(pausedWorkspace).mockResolvedValueOnce(activeWorkspace);
    vi.mocked(sessionApi.pauseSession).mockResolvedValue(pausedWorkspace.session);
    vi.mocked(sessionApi.resumeSession).mockResolvedValue(activeWorkspace.session);
    const store = useWorkspaceStore();

    await store.load("session-1");
    await store.pauseEpisode("session-1");
    expect(store.workspace?.session.status).toBe("paused");
    await store.resumeEpisode("session-1");

    expect(store.workspace?.session.status).toBe("active");
    expect(sessionApi.pauseSession).toHaveBeenCalledWith("session-1");
    expect(sessionApi.resumeSession).toHaveBeenCalledWith("session-1");
  });

  it("requests artwork for a scene and refreshes the workspace", async () => {
    const original = createTurn("scene-1", 1, "opening");
    const queued = { ...original, artworkStatus: "queued" as const };
    const workspace: StoryWorkspaceDto = {
      session: { id: "session-1", title: "Origin", genre: "Superhero", heroArchetype: "Guardian", heroName: "Ari", status: "active", moderationFailureCount: 0, createdAt: "2026-08-15T00:00:00Z", updatedAt: "2026-08-15T00:00:00Z" },
      turns: [original]
    };
    vi.mocked(sessionApi.getWorkspace).mockResolvedValueOnce(workspace).mockResolvedValueOnce({ ...workspace, turns: [queued] });
    vi.mocked(sceneApi.requestArtwork).mockResolvedValue(queued);
    const store = useWorkspaceStore();

    await store.load("session-1");
    await store.requestArtwork("session-1", original.id, true);

    expect(sceneApi.requestArtwork).toHaveBeenCalledWith("session-1", original.id, true);
    expect(store.workspace?.turns[0].artworkStatus).toBe("queued");
    expect(store.artworkSceneId).toBeNull();
  });
});

const createTurn = (id: string, sequenceNumber: number, storyBeat: SceneDto["storyBeat"]): SceneDto => ({
  id,
  sessionId: "session-1",
  sequenceNumber,
  choiceText: sequenceNumber === 1 ? "The story begins." : "Protect the city",
  narrativeText: "A story passage.",
  sceneSummary: "Summary",
  location: "Lumina",
  activeConflict: "Protect the city",
  storyStateSchemaVersion: 1,
  storyState: {},
  suggestedActions: ["Investigate", "Protect civilians"],
  storyBeat,
  isEpisodeComplete: false,
  artworkStatus: storyBeat === "opening" ? "queued" : "notRequested",
  artworkErrorCode: null,
  imageUrl: null,
  imageUrlExpiresAt: null,
  moderationStatus: "approved",
  moderationDetail: null,
  createdAt: "2026-08-15T00:00:00Z",
  updatedAt: "2026-08-15T00:00:00Z"
});