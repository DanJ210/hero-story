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
      <button type="submit">Create session</button>
    </form>
    <ul>
      <li v-for="session in sessionStore.sessions" :key="session.id">
        <RouterLink :to="`/sessions/${session.id}`">{{ session.title }} - {{ session.heroName }}</RouterLink>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { onMounted, reactive } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useSessionStore } from "../stores/sessionStore";
const title = import.meta.env.VITE_APP_TITLE ?? "Hero Story";
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const form = reactive({ title: "", genre: "", heroArchetype: "", heroName: "" });
onMounted(() => { void sessionStore.loadSessions(); });
const create = async () => { const session = await sessionStore.createSession(form); await router.push(`/sessions/${session.id}`); };
const logout = async () => { await authStore.logout(); await router.push("/login"); };
</script>
