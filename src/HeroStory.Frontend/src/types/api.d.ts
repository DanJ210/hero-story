export interface RegisterRequest { email: string; password: string; displayName: string; }
export interface LoginRequest { email: string; password: string; }
export interface TokenResponse { accessToken: string; refreshToken: string; expiresAtUtc: string; }
export interface SessionDto { id: string; title: string; genre: string; heroArchetype: string; heroName: string; status: string; moderationFailureCount: number; createdAt: string; updatedAt: string; }
export interface SessionListDto { id: string; title: string; genre: string; heroName: string; status: string; updatedAt: string; }
export type StoryBeat = "standard" | "opening" | "major" | "climax" | "conclusion";
export interface SceneDto { id: string; sessionId: string; sequenceNumber: number; choiceText: string; narrativeText: string; sceneSummary: string; location: string; activeConflict: string; storyStateSchemaVersion: number; storyState: Record<string, unknown>; suggestedActions: string[]; storyBeat: StoryBeat; isEpisodeComplete: boolean; imageUrl: string | null; imageUrlExpiresAt: string | null; moderationStatus: string; moderationDetail: string | null; createdAt: string; updatedAt: string; }
export interface SceneListDto { id: string; sequenceNumber: number; choiceText: string; imageUrl: string | null; moderationStatus: string; updatedAt: string; }
