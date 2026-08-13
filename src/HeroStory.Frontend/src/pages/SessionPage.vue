<template>
  <section>
    <h1>{{ sessionStore.currentSession?.title }}</h1>
    <p>{{ sessionStore.currentSession?.genre }} - {{ sessionStore.currentSession?.heroName }}</p>
    <ChoiceInput @submit="createScene" />
    <div class="scene-grid">
      <RouterLink v-for="scene in sceneStore.scenes" :key="scene.id" :to="`/sessions/${route.params.sessionId}/scenes/${scene.id}`">
        <SceneCard :scene="scene" />
      </RouterLink>
    </div>
  </section>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import ChoiceInput from "../components/ChoiceInput.vue";
import SceneCard from "../components/SceneCard.vue";
import { useSceneStore } from "../stores/sceneStore";
import { useSessionStore } from "../stores/sessionStore";
const route = useRoute();
const router = useRouter();
const sessionStore = useSessionStore();
const sceneStore = useSceneStore();
const sessionId = route.params.sessionId as string;
onMounted(async () => { await sessionStore.loadSession(sessionId); await sceneStore.loadScenes(sessionId); });
const createScene = async (choiceText: string) => { const scene = await sceneStore.createScene(sessionId, choiceText); await router.push(`/sessions/${sessionId}/scenes/${scene.id}`); };
</script>

<style scoped>
.scene-grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); }
</style>
