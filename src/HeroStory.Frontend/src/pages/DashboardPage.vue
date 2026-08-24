<template>
  <section>
    <header>
      <h1>{{ title }}</h1>
      <button @click="logout">Logout</button>
    </header>
    <form @submit.prevent="create">
      <input v-model="form.title" placeholder="Story title" />
      <input v-model="form.genre" placeholder="Genre" />
      <input v-model="form.heroArchetype" placeholder="Hero archetype" />
      <input v-model="form.heroName" placeholder="Hero name" />
      <label><input v-model="form.likenessEnabled" type="checkbox" :disabled="!portraitUploaded" /> Use my private portrait for automatic beat artwork</label>
      <button type="submit" :disabled="sessionStore.creating">{{ sessionStore.creating ? "Beginning story..." : "Begin story" }}</button>
    </form>
    <p v-if="creationError" role="alert">{{ creationError }}</p>
    <section class="portrait-panel" aria-labelledby="portrait-title">
      <h2 id="portrait-title">Hero portrait</h2>
      <div v-if="portrait" class="portrait-preview">
        <img :src="portrait.thumbnailUrl" alt="Your current private hero portrait" />
        <span>Current portrait</span>
      </div>
      <p>Your portrait stays private. It will not be used in artwork until you explicitly enable likeness generation.</p>
      <input type="file" accept="image/jpeg,image/png,image/webp" @change="selectPortrait" />
      <label><input v-model="portraitConsent" type="checkbox" /> I own or am authorized to use this image and consent to private storage.</label>
      <button type="button" :disabled="!portraitFile || !portraitConsent || portraitBusy" @click="uploadPortrait">
        {{ portraitBusy ? "Uploading..." : "Upload portrait" }}
      </button>
      <button v-if="portraitUploaded" type="button" :disabled="portraitBusy" @click="removePortrait">Remove portrait</button>
      <p v-if="portraitError" role="alert">{{ portraitError }}</p>
    </section>
    <ul>
      <li v-for="session in sessionStore.sessions" :key="session.id">
        <RouterLink :to="`/sessions/${session.id}`">{{ session.title }} - {{ session.heroName }}</RouterLink>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import axios from "axios";
import { onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useSessionStore } from "../stores/sessionStore";
import type { PortraitDto } from "../types/api";
import * as authApi from "../api/authApi";
const title = import.meta.env.VITE_APP_TITLE ?? "Hero Story";
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const form = reactive({ title: "", genre: "", heroArchetype: "", heroName: "", likenessEnabled: false });
const creationError = ref("");
const portraitFile = ref<File | null>(null);
const portraitConsent = ref(false);
const portraitUploaded = ref(false);
const portrait = ref<PortraitDto | null>(null);
const portraitBusy = ref(false);
const portraitError = ref("");
const selectPortrait = (event: Event) => { portraitFile.value = (event.target as HTMLInputElement).files?.[0] ?? null; };
const uploadPortrait = async () => {
  if (!portraitFile.value || !portraitConsent.value) return;
  portraitBusy.value = true;
  portraitError.value = "";
  try { portrait.value = await authApi.uploadPortrait(portraitFile.value, portraitConsent.value); portraitUploaded.value = true; }
  catch (error) { portraitError.value = axios.isAxiosError(error) && typeof error.response?.data?.error === "string" ? error.response.data.error : "The portrait could not be uploaded."; }
  finally { portraitBusy.value = false; }
};
const removePortrait = async () => {
  portraitBusy.value = true;
  try { await authApi.deletePortrait(); portraitUploaded.value = false; portrait.value = null; portraitFile.value = null; portraitConsent.value = false; }
  catch { portraitError.value = "The portrait could not be removed."; }
  finally { portraitBusy.value = false; }
};
onMounted(async () => {
  await sessionStore.loadSessions();
  try { portrait.value = await authApi.getPortrait(); portraitUploaded.value = true; }
  catch (error) { if (!axios.isAxiosError(error) || error.response?.status !== 404) portraitError.value = "The current portrait could not be loaded."; }
});
const create = async () => {
  creationError.value = "";
  try {
    const result = await sessionStore.createSession(form);
    await router.push(`/sessions/${result.session.id}`);
  } catch (error) {
    creationError.value = axios.isAxiosError(error) && typeof error.response?.data?.error === "string"
      ? error.response.data.error
      : "The story could not be started. Please try again.";
  }
};
const logout = async () => { await authStore.logout(); await router.push("/login"); };
</script>

<style scoped>
.portrait-panel { max-width: 520px; margin: 24px 0; padding: 16px; border: 1px solid #d3dad6; border-radius: 8px; }
.portrait-panel p { color: #5c706d; font-size: 13px; line-height: 1.45; }
.portrait-panel label { display: block; margin: 12px 0; font-size: 13px; }
.portrait-panel button { margin: 8px 8px 0 0; padding: 8px 12px; border: 1px solid #789490; border-radius: 6px; background: #eef2ec; color: #285c58; cursor: pointer; }
.portrait-panel button:disabled { opacity: 0.55; cursor: default; }
.portrait-preview { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; color: #285c58; font-size: 12px; font-weight: 700; }
.portrait-preview img { width: 72px; height: 72px; object-fit: cover; border: 2px solid #a8bbb6; border-radius: 50%; }
</style>
