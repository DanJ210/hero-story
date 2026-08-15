import { defineStore } from "pinia";
import * as sceneApi from "../api/sceneApi";
import * as sessionApi from "../api/sessionApi";
import type { StoryWorkspaceDto } from "../types/api";

export const useWorkspaceStore = defineStore("workspace", {
  state: () => ({ workspace: null as StoryWorkspaceDto | null, loading: false, generating: false }),
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
    }
  }
});