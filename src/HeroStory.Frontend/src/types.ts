export type StoryScene = {
  sequence: number;
  title: string;
  narrative: string;
  imagePrompt: string;
  imageUrl?: string | null;
};

export type Story = {
  id: string;
  heroName: string;
  setting: string;
  tone: string;
  prompt: string;
  status: string;
  coverImageUrl?: string | null;
  failureReason?: string | null;
  createdUtc: string;
  updatedUtc: string;
  scenes: StoryScene[];
};
