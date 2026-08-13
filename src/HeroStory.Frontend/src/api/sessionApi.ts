import httpClient from "./httpClient";
import type { SessionDto, SessionListDto } from "../types/api";

export const getSessions = async () => (await httpClient.get<SessionListDto[]>("/sessions")).data;
export const getSession = async (sessionId: string) => (await httpClient.get<SessionDto>(`/sessions/${sessionId}`)).data;
export const createSession = async (payload: { title: string; genre: string; heroArchetype: string; heroName: string }) =>
  (await httpClient.post<SessionDto>("/sessions", payload)).data;
