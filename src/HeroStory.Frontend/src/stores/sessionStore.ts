import { defineStore } from "pinia";
import * as sessionApi from "../api/sessionApi";
import type { SessionDto, SessionListDto } from "../types/api";

export const useSessionStore = defineStore("sessions", {
  state: () => ({ sessions: [] as SessionListDto[], currentSession: null as SessionDto | null, loading: false, creating: false }),
  actions: {
    async loadSessions() { this.loading = true; try { this.sessions = await sessionApi.getSessions(); } finally { this.loading = false; } },
    async loadSession(sessionId: string) { this.currentSession = await sessionApi.getSession(sessionId); },
    async createSession(payload: { title: string; genre: string; heroArchetype: string; heroName: string; likenessEnabled?: boolean }) { this.creating = true; try { const result = await sessionApi.createSession(payload); this.sessions = await sessionApi.getSessions(); this.currentSession = result.session; return result; } finally { this.creating = false; } }
  }
});
