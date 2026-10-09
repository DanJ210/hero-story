import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as sessionApi from "../api/sessionApi";
import * as authApi from "../api/authApi";
import { storySetupLimits } from "../utils/storySetup";
import DashboardPage from "./DashboardPage.vue";

const push = vi.fn();
vi.mock("vue-router", () => ({ useRouter: () => ({ push }), RouterLink: { template: "<a><slot /></a>" } }));
vi.mock("../api/sessionApi");
vi.mock("../api/authApi");

const mountPage = async () => {
  const wrapper = mount(DashboardPage, { global: { stubs: { RouterLink: true } } });
  await flushPromises();
  return wrapper;
};

const fill = async (wrapper: Awaited<ReturnType<typeof mountPage>>, values: Record<string, string>) => {
  for (const [key, value] of Object.entries(values)) await wrapper.find(`#story-${key}`).setValue(value);
};

describe("DashboardPage story setup", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.resetAllMocks();
    vi.mocked(sessionApi.getSessions).mockResolvedValue([]);
    vi.mocked(authApi.getPortrait).mockRejectedValue(new Error("none"));
  });

  it("labels every setup field and uses textareas for genre and hero archetype", async () => {
    const wrapper = await mountPage();

    for (const key of ["title", "genre", "heroArchetype", "heroName"]) {
      expect(wrapper.find(`label[for="story-${key}"]`).exists()).toBe(true);
    }
    expect(wrapper.find("#story-genre").element.tagName).toBe("TEXTAREA");
    expect(wrapper.find("#story-heroArchetype").element.tagName).toBe("TEXTAREA");
    expect(wrapper.find("#story-genre").attributes("maxlength")).toBeUndefined();
  });

  it("shows length feedback and blocks oversized text without truncating it", async () => {
    const wrapper = await mountPage();
    const oversizedGenre = "g".repeat(storySetupLimits.genre + 1);
    await fill(wrapper, { title: "Origin", genre: oversizedGenre, heroArchetype: "Guardian", heroName: "Ari" });

    expect(wrapper.find("#story-genre-count").text()).toBe("501 / 500");
    await wrapper.find("form").trigger("submit");
    await flushPromises();

    expect(sessionApi.createSession).not.toHaveBeenCalled();
    expect(wrapper.find("#story-genre-error").text()).toBe("Genre must be 500 characters or fewer.");
    expect((wrapper.find("#story-genre").element as HTMLTextAreaElement).value).toBe(oversizedGenre);
  });

  it("submits text at the exact limits unchanged", async () => {
    vi.mocked(sessionApi.createSession).mockResolvedValue({ session: { id: "session-1" } } as never);
    const wrapper = await mountPage();
    const values = {
      title: "t".repeat(storySetupLimits.title),
      genre: "g".repeat(storySetupLimits.genre),
      heroArchetype: "a".repeat(storySetupLimits.heroArchetype),
      heroName: "n".repeat(storySetupLimits.heroName)
    };
    await fill(wrapper, values);

    await wrapper.find("form").trigger("submit");
    await flushPromises();

    expect(sessionApi.createSession).toHaveBeenCalledWith(expect.objectContaining(values));
    expect(push).toHaveBeenCalledWith("/sessions/session-1");
  });
});