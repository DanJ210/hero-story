<template>
  <section>
    <h1>Login</h1>
    <form @submit.prevent="handleLogin">
      <input v-model="email" type="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password" />
      <button type="submit">Login</button>
    </form>
    <button v-if="developmentAuthEnabled" type="button" :disabled="authStore.loading" @click="handleDevelopmentLogin">
      Continue as development user
    </button>
    <p><RouterLink to="/register">Create an account</RouterLink></p>
  </section>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
const router = useRouter();
const authStore = useAuthStore();
const developmentAuthEnabled = import.meta.env.VITE_DEV_AUTH_ENABLED === "true";
const email = ref("");
const password = ref("");
const handleLogin = async () => { await authStore.login(email.value, password.value); await router.push("/"); };
const handleDevelopmentLogin = async () => { await authStore.developmentLogin(); await router.push("/"); };
</script>
