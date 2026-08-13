<template>
  <article class="scene-card">
    <h3>Scene {{ scene.sequenceNumber }}</h3>
    <p>{{ scene.choiceText }}</p>
    <p>Status: {{ scene.moderationStatus }}</p>
    <img v-if="scene.imageUrl" :src="scene.imageUrl" alt="Generated scene artwork" />
  </article>
</template>

<script setup lang="ts">
import type { SceneListDto } from "../types/api";
defineProps<{ scene: SceneListDto }>();
</script>

<style scoped>
.scene-card { border: 1px solid #ccc; border-radius: 8px; padding: 1rem; }
img { max-width: 100%; margin-top: 0.5rem; }
</style>
