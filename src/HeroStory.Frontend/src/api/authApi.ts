import httpClient from "./httpClient";
import type { LoginRequest, PortraitDto, RegisterRequest, TokenResponse } from "../types/api";

export const register = async (payload: RegisterRequest) => httpClient.post("/auth/register", payload);
export const login = async (payload: LoginRequest) => (await httpClient.post<TokenResponse>("/auth/login", payload)).data;
export const developmentLogin = async () => (await httpClient.post<TokenResponse>("/auth/dev-login")).data;
export const refresh = async (refreshToken: string) => (await httpClient.post<TokenResponse>("/auth/refresh", { refreshToken })).data;
export const logout = async (refreshToken: string) => httpClient.post("/auth/logout", { refreshToken });
export const uploadPortrait = async (file: File, consentGranted: boolean) => {
	const form = new FormData();
	form.append("file", file);
	form.append("consentGranted", String(consentGranted));
	return (await httpClient.post<PortraitDto>("/profile/portrait", form)).data;
};
export const deletePortrait = async () => httpClient.delete("/profile/portrait");
