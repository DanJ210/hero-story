<template>
  <article class="scene-card">
    <h3>Scene {{ scene.sequenceNumber }}</h3>
    <p>{{ scene.choiceText }}</p>
    <p>Status: {{ scene.moderationStatus }}</p>
    <img v-if="scene.imageUrl" :src="scene.imageUrl" alt="Generated scene artwork" />
    <p v-else-if="isArtworkPending(scene.artworkStatus)">Artwork pending</p>
    <p v-else-if="scene.artworkStatus === 'failed' || scene.artworkStatus === 'poisoned'">Artwork unavailable</p>
  </article>
</template>

<script setup lang="ts">
import type { SceneListDto } from "../types/api";
import { isArtworkPending } from "../utils/artworkStatus";
defineProps<{ scene: SceneListDto }>();
</script>

<style scoped>
.scene-card { border: 1px solid #ccc; border-radius: 8px; padding: 1rem; }
img { max-width: 100%; margin-top: 0.5rem; }
</style>
