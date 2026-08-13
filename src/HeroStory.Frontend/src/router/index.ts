import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import LoginPage from "../pages/LoginPage.vue";
import RegisterPage from "../pages/RegisterPage.vue";
import DashboardPage from "../pages/DashboardPage.vue";
import SessionPage from "../pages/SessionPage.vue";
import ScenePage from "../pages/ScenePage.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/login", component: LoginPage },
    { path: "/register", component: RegisterPage },
    { path: "/", component: DashboardPage, meta: { requiresAuth: true } },
    { path: "/sessions/:sessionId", component: SessionPage, meta: { requiresAuth: true } },
    { path: "/sessions/:sessionId/scenes/:sceneId", component: ScenePage, meta: { requiresAuth: true } }
  ]
});

router.beforeEach((to) => {
  const authStore = useAuthStore();
  if (to.meta.requiresAuth && !authStore.isAuthenticated) return "/login";
  if ((to.path === "/login" || to.path === "/register") && authStore.isAuthenticated) return "/";
  return true;
});

export default router;
