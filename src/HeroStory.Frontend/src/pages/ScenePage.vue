<template>
  <section v-if="sceneStore.currentScene">
    <h1>Scene {{ sceneStore.currentScene.sequenceNumber }}</h1>
    <p>{{ sceneStore.currentScene.narrativeText }}</p>
    <p v-if="sceneStore.currentScene.moderationDetail">Moderation: {{ sceneStore.currentScene.moderationDetail }}</p>
    <img v-if="sceneStore.currentScene.imageUrl" :src="sceneStore.currentScene.imageUrl" alt="Generated artwork" />
    <p v-else-if="isArtworkPending(sceneStore.currentScene.artworkStatus)">Artwork is being generated...</p>
    <p v-else-if="sceneStore.currentScene.artworkStatus === 'failed'">{{ artworkErrorMessage(sceneStore.currentScene.artworkErrorCode) }}</p>
    <p v-else-if="sceneStore.currentScene.artworkStatus === 'poisoned'">{{ artworkErrorMessage(sceneStore.currentScene.artworkErrorCode) }}</p>
    <p v-else-if="sceneStore.currentScene.artworkStatus === 'completed'">Artwork is temporarily unavailable.</p>
    <p><RouterLink :to="`/sessions/${sessionId}`">Continue story</RouterLink></p>
  </section>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted } from "vue";
import { useRoute } from "vue-router";
import { useSceneStore } from "../stores/sceneStore";
import { isArtworkPending } from "../utils/artworkStatus";
import { artworkErrorMessage } from "../utils/artworkStatus";
const route = useRoute();
const sceneStore = useSceneStore();
const sessionId = route.params.sessionId as string;
const sceneId = route.params.sceneId as string;
const pollIntervalMs = Number(import.meta.env.VITE_IMAGE_POLL_INTERVAL_MS ?? 3000);
let timer: number | undefined;
const pollScene = async () => {
  const scene = await sceneStore.loadScene(sessionId, sceneId);
  if (scene && !isArtworkPending(scene.artworkStatus) && timer !== undefined) { window.clearInterval(timer); timer = undefined; }
};
onMounted(async () => {
  await pollScene();
  if (sceneStore.currentScene && isArtworkPending(sceneStore.currentScene.artworkStatus)) { timer = window.setInterval(() => { void pollScene(); }, pollIntervalMs); }
});
onBeforeUnmount(() => { if (timer !== undefined) window.clearInterval(timer); });
</script>
