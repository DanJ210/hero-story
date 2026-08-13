import { defineStore } from "pinia";
import * as authApi from "../api/authApi";

const ACCESS_TOKEN_KEY = "hero-story.access-token";
const REFRESH_TOKEN_KEY = "hero-story.refresh-token";

export const useAuthStore = defineStore("auth", {
  state: () => ({ accessToken: localStorage.getItem(ACCESS_TOKEN_KEY) ?? "", refreshToken: localStorage.getItem(REFRESH_TOKEN_KEY) ?? "", loading: false }),
  getters: { isAuthenticated: (state) => Boolean(state.accessToken) },
  actions: {
    async register(email: string, password: string, displayName: string) { await authApi.register({ email, password, displayName }); },
    async login(email: string, password: string) { this.loading = true; try { const response = await authApi.login({ email, password }); this.setTokens(response.accessToken, response.refreshToken); } finally { this.loading = false; } },
    async refresh() { if (!this.refreshToken) throw new Error("No refresh token available."); const response = await authApi.refresh(this.refreshToken); this.setTokens(response.accessToken, response.refreshToken); },
    async logout() { if (this.refreshToken) await authApi.logout(this.refreshToken); this.clearTokens(); },
    setTokens(accessToken: string, refreshToken: string) { this.accessToken = accessToken; this.refreshToken = refreshToken; localStorage.setItem(ACCESS_TOKEN_KEY, accessToken); localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken); },
    clearTokens() { this.accessToken = ""; this.refreshToken = ""; localStorage.removeItem(ACCESS_TOKEN_KEY); localStorage.removeItem(REFRESH_TOKEN_KEY); }
  }
});
