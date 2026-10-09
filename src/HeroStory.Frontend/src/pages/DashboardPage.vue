<template>
  <section>
    <header>
      <h1>{{ title }}</h1>
      <button @click="logout">Logout</button>
    </header>
    <form class="story-setup" novalidate @submit.prevent="create">
      <div v-for="field in setupFields" :key="field.key" class="setup-field">
        <label :for="`story-${field.key}`">{{ field.label }}</label>
        <p :id="`story-${field.key}-help`" class="field-help">{{ field.help }}</p>
        <textarea
          v-if="field.multiline"
          :id="`story-${field.key}`"
          v-model="form[field.key]"
          rows="3"
          :aria-describedby="`story-${field.key}-help story-${field.key}-count${fieldErrors[field.key] ? ` story-${field.key}-error` : ''}`"
          :aria-invalid="fieldErrors[field.key] ? 'true' : undefined"
          required
        ></textarea>
        <input
          v-else
          :id="`story-${field.key}`"
          v-model="form[field.key]"
          type="text"
          :aria-describedby="`story-${field.key}-help story-${field.key}-count${fieldErrors[field.key] ? ` story-${field.key}-error` : ''}`"
          :aria-invalid="fieldErrors[field.key] ? 'true' : undefined"
          required
        />
        <span :id="`story-${field.key}-count`" class="field-count" :class="{ over: form[field.key].length > storySetupLimits[field.key] }">{{ form[field.key].length }} / {{ storySetupLimits[field.key] }}</span>
        <p v-if="fieldErrors[field.key]" :id="`story-${field.key}-error`" class="field-error" role="alert">{{ fieldErrors[field.key] }}</p>
      </div>
      <label><input v-model="form.likenessEnabled" type="checkbox" :disabled="!portraitConsentValid" /> Use my private portrait for automatic beat artwork</label>
      <button type="submit" :disabled="sessionStore.creating">{{ sessionStore.creating ? "Beginning story..." : "Begin story" }}</button>
    </form>
    <p v-if="creationError" role="alert">{{ creationError }}</p>
    <section class="portrait-panel" aria-labelledby="portrait-title">
      <h2 id="portrait-title">Hero portrait</h2>
      <div v-if="portrait" class="portrait-preview">
        <img v-if="portraitPreviewUrl" :src="portraitPreviewUrl" alt="Your current private hero portrait" />
        <span>Current portrait</span>
      </div>
      <p>Your portrait stays private. It is used only for story artwork when you enable likeness for a story or an individual scene. Generated artwork remains part of the story after portrait removal.</p>
      <input type="file" accept="image/jpeg,image/png,image/webp" @change="selectPortrait" />
      <p v-if="portrait && !portrait.consentValid" role="status">This portrait predates versioned likeness consent. Replace it and grant consent before using likeness artwork.</p>
      <label><input v-model="portraitConsent" type="checkbox" /> I own or am authorized to use this image. I consent to private storage and to OpenAI image generation using it only as a reference for my story artwork.</label>
      <button type="button" :disabled="!portraitFile || !portraitConsent || portraitBusy" @click="uploadPortrait">
        {{ portraitBusy ? "Uploading..." : portraitUploaded ? "Replace portrait" : "Upload portrait" }}
      </button>
      <button v-if="portraitUploaded" type="button" :disabled="portraitBusy" @click="disablePortrait">Disable likeness</button>
      <button v-if="portraitUploaded" type="button" :disabled="portraitBusy" @click="removePortrait">Delete portrait</button>
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
import { computed, onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useSessionStore } from "../stores/sessionStore";
import type { PortraitDto } from "../types/api";
import * as authApi from "../api/authApi";
import { extractApiErrorMessage, storySetupLabels, storySetupLimits, validateStorySetup, type StorySetupErrors, type StorySetupField } from "../utils/storySetup";
const title = import.meta.env.VITE_APP_TITLE ?? "Hero Story";
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const form = reactive({ title: "", genre: "", heroArchetype: "", heroName: "", likenessEnabled: false });
const creationError = ref("");
const fieldErrors = ref<StorySetupErrors>({});
const setupFields: { key: StorySetupField; label: string; help: string; multiline: boolean }[] = [
  { key: "title", label: storySetupLabels.title, help: "The name of this story.", multiline: false },
  { key: "genre", label: storySetupLabels.genre, help: "Describe the genre, tone, or setting style. A short phrase or a few sentences.", multiline: true },
  { key: "heroArchetype", label: storySetupLabels.heroArchetype, help: "Describe the kind of hero: role, powers, temperament, or background.", multiline: true },
  { key: "heroName", label: storySetupLabels.heroName, help: "What your hero is called in the story.", multiline: false }
];
const portraitFile = ref<File | null>(null);
const portraitConsent = ref(false);
const portraitUploaded = ref(false);
const portrait = ref<PortraitDto | null>(null);
const portraitConsentValid = computed(() => portrait.value?.consentValid === true);
const portraitPreviewUrl = ref("");
const portraitBusy = ref(false);
const portraitError = ref("");
const selectPortrait = (event: Event) => { portraitFile.value = (event.target as HTMLInputElement).files?.[0] ?? null; };
const setPortraitPreview = (blob: Blob | null) => {
  if (portraitPreviewUrl.value) URL.revokeObjectURL(portraitPreviewUrl.value);
  portraitPreviewUrl.value = blob ? URL.createObjectURL(blob) : "";
};
const uploadPortrait = async () => {
  if (!portraitFile.value || !portraitConsent.value) return;
  portraitBusy.value = true;
  portraitError.value = "";
  try {
    portrait.value = await authApi.uploadPortrait(portraitFile.value, portraitConsent.value);
    portraitUploaded.value = true;
    try { setPortraitPreview(await authApi.getPortraitContent()); }
    catch { setPortraitPreview(null); portraitError.value = "The portrait was saved, but its private preview could not be loaded."; }
  }
  catch (error) { portraitError.value = axios.isAxiosError(error) && typeof error.response?.data?.error === "string" ? error.response.data.error : "The portrait could not be uploaded."; }
  finally { portraitBusy.value = false; }
};
const removePortrait = async () => {
  portraitBusy.value = true;
  portraitError.value = "";
  try { await authApi.deletePortrait(); portraitUploaded.value = false; portrait.value = null; setPortraitPreview(null); portraitFile.value = null; portraitConsent.value = false; form.likenessEnabled = false; }
  catch { portraitError.value = "The portrait could not be removed."; }
  finally { portraitBusy.value = false; }
};
const disablePortrait = async () => {
  portraitBusy.value = true;
  portraitError.value = "";
  try { await authApi.disablePortrait(); portraitUploaded.value = false; portrait.value = null; setPortraitPreview(null); form.likenessEnabled = false; }
  catch { portraitError.value = "Likeness could not be disabled."; }
  finally { portraitBusy.value = false; }
};
onMounted(async () => {
  await sessionStore.loadSessions();
  try {
    portrait.value = await authApi.getPortrait();
    portraitUploaded.value = true;
    try { setPortraitPreview(await authApi.getPortraitContent()); }
    catch { portraitError.value = "The current portrait preview could not be loaded."; }
  }
  catch (error) { if (!axios.isAxiosError(error) || error.response?.status !== 404) portraitError.value = "The current portrait could not be loaded."; }
});
onBeforeUnmount(() => setPortraitPreview(null));
const create = async () => {
  creationError.value = "";
  fieldErrors.value = validateStorySetup(form);
  if (Object.keys(fieldErrors.value).length > 0) return;
  try {
    const result = await sessionStore.createSession(form);
    await router.push(`/sessions/${result.session.id}`);
  } catch (error) {
    creationError.value = extractApiErrorMessage(error, "The story could not be started. Please try again.");
  }
};
const logout = async () => { await authStore.logout(); await router.push("/login"); };
</script>

<style scoped>
.story-setup { display: grid; gap: 14px; max-width: 520px; margin: 16px 0; }
.setup-field { display: grid; gap: 4px; }
.setup-field label { font-weight: 700; font-size: 14px; }
.setup-field input, .setup-field textarea { width: 100%; box-sizing: border-box; font: inherit; }
.setup-field textarea { resize: vertical; }
.field-help { margin: 0; color: #5c706d; font-size: 12px; }
.field-count { justify-self: end; color: #5c706d; font-size: 12px; }
.field-count.over { color: #a3312a; font-weight: 700; }
.field-error { margin: 0; color: #a3312a; font-size: 13px; }
.portrait-panel { max-width: 520px; margin: 24px 0; padding: 16px; border: 1px solid #d3dad6; border-radius: 8px; }
.portrait-panel p { color: #5c706d; font-size: 13px; line-height: 1.45; }
.portrait-panel label { display: block; margin: 12px 0; font-size: 13px; }
.portrait-panel button { margin: 8px 8px 0 0; padding: 8px 12px; border: 1px solid #789490; border-radius: 6px; background: #eef2ec; color: #285c58; cursor: pointer; }
.portrait-panel button:disabled { opacity: 0.55; cursor: default; }
.portrait-preview { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; color: #285c58; font-size: 12px; font-weight: 700; }
.portrait-preview img { width: 72px; height: 72px; object-fit: cover; border: 2px solid #a8bbb6; border-radius: 50%; }
</style>
