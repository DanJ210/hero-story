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
const title = import.meta.env.VITE_APP_TITLE ?? "Hero Story";
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const form = reactive({ title: "", genre: "", heroArchetype: "", heroName: "" });
const creationError = ref("");
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
