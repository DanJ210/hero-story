import httpClient from "./httpClient";
import type { LoginRequest, RegisterRequest, TokenResponse } from "../types/api";

export const register = async (payload: RegisterRequest) => httpClient.post("/auth/register", payload);
export const login = async (payload: LoginRequest) => (await httpClient.post<TokenResponse>("/auth/login", payload)).data;
export const refresh = async (refreshToken: string) => (await httpClient.post<TokenResponse>("/auth/refresh", { refreshToken })).data;
export const logout = async (refreshToken: string) => httpClient.post("/auth/logout", { refreshToken });
