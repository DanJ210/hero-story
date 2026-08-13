<template>
  <form @submit.prevent="submit">
    <textarea v-model="choiceText" placeholder="Choose the hero's next action"></textarea>
    <button type="submit">Create scene</button>
  </form>
</template>

<script setup lang="ts">
import { ref } from "vue";
const emit = defineEmits<{ submit: [value: string] }>();
const choiceText = ref("");
const submit = () => {
  if (!choiceText.value.trim()) return;
  emit("submit", choiceText.value.trim());
  choiceText.value = "";
};
</script>

<style scoped>
textarea { width: 100%; min-height: 120px; margin-bottom: 0.75rem; }
</style>
