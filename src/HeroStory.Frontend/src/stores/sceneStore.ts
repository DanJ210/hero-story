import { defineStore } from "pinia";
import * as sceneApi from "../api/sceneApi";
import type { SceneDto, SceneListDto } from "../types/api";

export const useSceneStore = defineStore("scenes", {
  state: () => ({ scenes: [] as SceneListDto[], currentScene: null as SceneDto | null, loading: false }),
  actions: {
    async loadScenes(sessionId: string) { this.loading = true; try { this.scenes = await sceneApi.getScenes(sessionId); } finally { this.loading = false; } },
    async loadScene(sessionId: string, sceneId: string) { this.currentScene = await sceneApi.getScene(sessionId, sceneId); return this.currentScene; },
    async createScene(sessionId: string, choiceText: string) { const scene = await sceneApi.createScene(sessionId, choiceText); await this.loadScenes(sessionId); this.currentScene = scene; return scene; }
  }
});
