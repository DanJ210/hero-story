<template>
  <section>
    <h1>Register</h1>
    <form @submit.prevent="handleRegister">
      <input v-model="displayName" placeholder="Display name" />
      <input v-model="email" type="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password" />
      <button type="submit">Register</button>
    </form>
    <p><RouterLink to="/login">Back to login</RouterLink></p>
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
const router = useRouter();
const authStore = useAuthStore();
const displayName = ref("");
const email = ref("");
const password = ref("");
const handleRegister = async () => { await authStore.register(email.value, password.value, displayName.value); await router.push("/login"); };
</script>
