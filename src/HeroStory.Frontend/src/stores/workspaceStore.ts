import { defineStore } from "pinia";
import * as sceneApi from "../api/sceneApi";
import * as sessionApi from "../api/sessionApi";
import type { StoryWorkspaceDto } from "../types/api";

export const useWorkspaceStore = defineStore("workspace", {
  state: () => ({ workspace: null as StoryWorkspaceDto | null, loading: false, generating: false, transitioning: false, artworkSceneId: null as string | null }),
  getters: {
    latestTurn: (state) => state.workspace?.turns[state.workspace.turns.length - 1] ?? null
  },
  actions: {
    async load(sessionId: string) {
      this.loading = true;
      try {
        this.workspace = await sessionApi.getWorkspace(sessionId);
        return this.workspace;
      } finally {
        this.loading = false;
      }
    },
    async refresh(sessionId: string) {
      this.workspace = await sessionApi.getWorkspace(sessionId);
      return this.workspace;
    },
    async continueStory(sessionId: string, choiceText: string) {
      this.generating = true;
      try {
        const turn = await sceneApi.createScene(sessionId, choiceText);
        if (this.workspace) this.workspace.turns.push(turn);
        return turn;
      } finally {
        this.generating = false;
      }
    },
    async reviseLatestTurn(sessionId: string, sceneId: string, choiceText: string) {
      this.generating = true;
      try {
        const replacement = await sceneApi.reviseScene(sessionId, sceneId, choiceText);
        await this.refresh(sessionId);
        return replacement;
      } finally {
        this.generating = false;
      }
    },
    async pauseEpisode(sessionId: string) {
      this.transitioning = true;
      try {
        await sessionApi.pauseSession(sessionId);
        await this.refresh(sessionId);
      } finally {
        this.transitioning = false;
      }
    },
    async resumeEpisode(sessionId: string) {
      this.transitioning = true;
      try {
        await sessionApi.resumeSession(sessionId);
        await this.refresh(sessionId);
      } finally {
        this.transitioning = false;
      }
    },
    async concludeEpisode(sessionId: string) {
      this.generating = true;
      try {
        const conclusion = await sessionApi.concludeEpisode(sessionId);
        await this.refresh(sessionId);
        return conclusion;
      } finally {
        this.generating = false;
      }
    },
    async requestArtwork(sessionId: string, sceneId: string) {
      this.artworkSceneId = sceneId;
      try {
        const scene = await sceneApi.requestArtwork(sessionId, sceneId);
        await this.refresh(sessionId);
        return scene;
      } finally {
        this.artworkSceneId = null;
      }
    }
  }
});