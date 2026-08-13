import { defineStore } from "pinia";
import * as sessionApi from "../api/sessionApi";
import type { SessionDto, SessionListDto } from "../types/api";

export const useSessionStore = defineStore("sessions", {
  state: () => ({ sessions: [] as SessionListDto[], currentSession: null as SessionDto | null, loading: false }),
  actions: {
    async loadSessions() { this.loading = true; try { this.sessions = await sessionApi.getSessions(); } finally { this.loading = false; } },
    async loadSession(sessionId: string) { this.currentSession = await sessionApi.getSession(sessionId); },
    async createSession(payload: { title: string; genre: string; heroArchetype: string; heroName: string }) { const session = await sessionApi.createSession(payload); await this.loadSessions(); this.currentSession = session; return session; }
  }
});
