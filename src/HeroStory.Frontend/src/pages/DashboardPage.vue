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
      <button type="submit" :disabled="sessionStore.creating">{{ sessionStore.creating ? "Beginning story..." : "Begin story" }}</button>
    </form>
    <p v-if="creationError" role="alert">{{ creationError }}</p>
    <section class="portrait-panel" aria-labelledby="portrait-title">
      <h2 id="portrait-title">Hero portrait</h2>
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
import * as authApi from "../api/authApi";
const title = import.meta.env.VITE_APP_TITLE ?? "Hero Story";
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const form = reactive({ title: "", genre: "", heroArchetype: "", heroName: "" });
const creationError = ref("");
const portraitFile = ref<File | null>(null);
const portraitConsent = ref(false);
const portraitUploaded = ref(false);
const portraitBusy = ref(false);
const portraitError = ref("");
const selectPortrait = (event: Event) => { portraitFile.value = (event.target as HTMLInputElement).files?.[0] ?? null; };
const uploadPortrait = async () => {
  if (!portraitFile.value || !portraitConsent.value) return;
  portraitBusy.value = true;
  portraitError.value = "";
  try { await authApi.uploadPortrait(portraitFile.value, portraitConsent.value); portraitUploaded.value = true; }
  catch (error) { portraitError.value = axios.isAxiosError(error) && typeof error.response?.data?.error === "string" ? error.response.data.error : "The portrait could not be uploaded."; }
  finally { portraitBusy.value = false; }
};
const removePortrait = async () => {
  portraitBusy.value = true;
  try { await authApi.deletePortrait(); portraitUploaded.value = false; portraitFile.value = null; portraitConsent.value = false; }
  catch { portraitError.value = "The portrait could not be removed."; }
  finally { portraitBusy.value = false; }
};
onMounted(() => { void sessionStore.loadSessions(); });
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
