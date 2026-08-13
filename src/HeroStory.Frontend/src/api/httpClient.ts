import axios from "axios";
import { useAuthStore } from "../stores/authStore";

const httpClient = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL });

httpClient.interceptors.request.use((config) => {
  const authStore = useAuthStore();
  if (authStore.accessToken) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${authStore.accessToken}`;
  }
  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const authStore = useAuthStore();
    const originalRequest = error.config as (typeof error.config & { _retry?: boolean }) | undefined;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && authStore.refreshToken) {
      originalRequest._retry = true;
      await authStore.refresh();
      originalRequest.headers = originalRequest.headers ?? {};
      (originalRequest.headers as any).Authorization = `Bearer ${authStore.accessToken}`;
      return httpClient(originalRequest);
    }

    return Promise.reject(error);
  }
);

export default httpClient;
