import { AxiosError, AxiosHeaders } from "axios";
import { describe, expect, it } from "vitest";
import { extractApiErrorMessage, storySetupLimits, validateStorySetup } from "./storySetup";

const atLimit = () => ({
  title: "t".repeat(storySetupLimits.title),
  genre: "g".repeat(storySetupLimits.genre),
  heroArchetype: "a".repeat(storySetupLimits.heroArchetype),
  heroName: "n".repeat(storySetupLimits.heroName)
});

const axiosErrorWith = (data: unknown) => new AxiosError("Bad request", "ERR_BAD_REQUEST", undefined, undefined, {
  data,
  status: 400,
  statusText: "Bad Request",
  headers: {},
  config: { headers: new AxiosHeaders() }
});

describe("validateStorySetup", () => {
  it("matches the API limits", () => {
    expect(storySetupLimits).toEqual({ title: 200, genre: 500, heroArchetype: 1000, heroName: 100 });
  });

  it("accepts every field at its exact limit", () => {
    expect(validateStorySetup(atLimit())).toEqual({});
  });

  it("rejects fields one character over the limit", () => {
    const values = atLimit();
    values.genre += "g";
    values.heroArchetype += "a";

    expect(validateStorySetup(values)).toEqual({
      genre: "Genre must be 500 characters or fewer.",
      heroArchetype: "Hero archetype must be 1000 characters or fewer."
    });
  });

  it("requires non-blank values", () => {
    expect(validateStorySetup({ title: "  ", genre: "", heroArchetype: "Guardian", heroName: "Ari" })).toEqual({
      title: "Title is required.",
      genre: "Genre is required."
    });
  });
});

describe("extractApiErrorMessage", () => {
  it("reads the API error field", () => {
    expect(extractApiErrorMessage(axiosErrorWith({ error: "Genre must be 500 characters or fewer.", status: 400 }), "fallback"))
      .toBe("Genre must be 500 characters or fewer.");
  });

  it("reads the first validation problem message", () => {
    expect(extractApiErrorMessage(axiosErrorWith({ title: "One or more validation errors occurred.", errors: { Genre: ["The Genre field is required."] } }), "fallback"))
      .toBe("The Genre field is required.");
  });

  it("falls back for unknown errors", () => {
    expect(extractApiErrorMessage(new Error("network"), "fallback")).toBe("fallback");
  });
});