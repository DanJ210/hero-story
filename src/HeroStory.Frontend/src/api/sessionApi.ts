import httpClient from "./httpClient";
import type { CreateStorySessionResponse, SceneDto, SessionDto, SessionListDto, StoryWorkspaceDto } from "../types/api";

export const getSessions = async () => (await httpClient.get<SessionListDto[]>("/sessions")).data;
export const getSession = async (sessionId: string) => (await httpClient.get<SessionDto>(`/sessions/${sessionId}`)).data;
export const getWorkspace = async (sessionId: string) => (await httpClient.get<StoryWorkspaceDto>(`/sessions/${sessionId}/workspace`)).data;
export const createSession = async (payload: { title: string; genre: string; heroArchetype: string; heroName: string; likenessEnabled?: boolean }) =>
  (await httpClient.post<CreateStorySessionResponse>("/sessions", payload)).data;
export const pauseSession = async (sessionId: string) => (await httpClient.post<SessionDto>(`/sessions/${sessionId}/pause`)).data;
export const resumeSession = async (sessionId: string) => (await httpClient.post<SessionDto>(`/sessions/${sessionId}/resume`)).data;
export const concludeEpisode = async (sessionId: string) => (await httpClient.post<SceneDto>(`/sessions/${sessionId}/conclusion`)).data;
