import { Story } from './types';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api';

export type CreateStoryPayload = {
  heroName: string;
  setting: string;
  tone: string;
  prompt: string;
};

export async function createStory(payload: CreateStoryPayload): Promise<Story> {
  const response = await fetch(`${apiBaseUrl}/stories`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error('Failed to create story.');
  }

  return response.json() as Promise<Story>;
}

export async function listStories(): Promise<Story[]> {
  const response = await fetch(`${apiBaseUrl}/stories`);
  if (!response.ok) {
    throw new Error('Failed to load stories.');
  }

  return response.json() as Promise<Story[]>;
}
