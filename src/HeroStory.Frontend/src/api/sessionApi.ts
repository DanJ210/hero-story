import httpClient from "./httpClient";
import type { CreateStorySessionResponse, SessionDto, SessionListDto, StoryWorkspaceDto } from "../types/api";

export const getSessions = async () => (await httpClient.get<SessionListDto[]>("/sessions")).data;
export const getSession = async (sessionId: string) => (await httpClient.get<SessionDto>(`/sessions/${sessionId}`)).data;
export const getWorkspace = async (sessionId: string) => (await httpClient.get<StoryWorkspaceDto>(`/sessions/${sessionId}/workspace`)).data;
export const createSession = async (payload: { title: string; genre: string; heroArchetype: string; heroName: string }) =>
  (await httpClient.post<CreateStorySessionResponse>("/sessions", payload)).data;
