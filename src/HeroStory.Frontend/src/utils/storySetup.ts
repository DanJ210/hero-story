import axios from "axios";

/** Mirrors StorySessionFieldLimits in HeroStory.Core; the API enforces the same limits. */
export const storySetupLimits = {
  title: 200,
  genre: 500,
  heroArchetype: 1000,
  heroName: 100
} as const;

export type StorySetupField = keyof typeof storySetupLimits;
export type StorySetupValues = Record<StorySetupField, string>;
export type StorySetupErrors = Partial<Record<StorySetupField, string>>;

export const storySetupLabels: Record<StorySetupField, string> = {
  title: "Title",
  genre: "Genre",
  heroArchetype: "Hero archetype",
  heroName: "Hero name"
};

/** Returns a readable error per invalid field. Text is never trimmed or truncated. */
export const validateStorySetup = (values: StorySetupValues): StorySetupErrors => {
  const errors: StorySetupErrors = {};
  for (const field of Object.keys(storySetupLimits) as StorySetupField[]) {
    const value = values[field];
    const max = storySetupLimits[field];
    if (value.trim().length === 0) errors[field] = `${storySetupLabels[field]} is required.`;
    else if (value.length > max) errors[field] = `${storySetupLabels[field]} must be ${max} characters or fewer.`;
  }
  return errors;
};

/** Reads the API's { error } body or the first ASP.NET validation problem message. */
export const extractApiErrorMessage = (error: unknown, fallback: string): string => {
  if (!axios.isAxiosError(error)) return fallback;
  const data: unknown = error.response?.data;
  if (data && typeof data === "object") {
    const body = data as { error?: unknown; errors?: unknown };
    if (typeof body.error === "string" && body.error.length > 0) return body.error;
    if (body.errors && typeof body.errors === "object") {
      const first = Object.values(body.errors as Record<string, unknown>).flat().find((message) => typeof message === "string");
      if (typeof first === "string") return first;
    }
  }
  return fallback;
};