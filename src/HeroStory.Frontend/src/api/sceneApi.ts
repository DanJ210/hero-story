import httpClient from "./httpClient";
import type { SceneDto, SceneListDto } from "../types/api";

export const getScenes = async (sessionId: string) => (await httpClient.get<SceneListDto[]>(`/sessions/${sessionId}/scenes`)).data;
export const getScene = async (sessionId: string, sceneId: string) => (await httpClient.get<SceneDto>(`/sessions/${sessionId}/scenes/${sceneId}`)).data;
export const createScene = async (sessionId: string, choiceText: string) =>
  (await httpClient.post<SceneDto>(`/sessions/${sessionId}/scenes`, { choiceText })).data;
export const requestArtwork = async (sessionId: string, sceneId: string) =>
  (await httpClient.post<SceneDto>(`/sessions/${sessionId}/scenes/${sceneId}/artwork`)).data;
export const reviseScene = async (sessionId: string, sceneId: string, choiceText: string) =>
  (await httpClient.post<SceneDto>(`/sessions/${sessionId}/scenes/${sceneId}/revisions`, { choiceText })).data;
