<template>
  <div class="story-workspace">
    <button v-if="drawerOpen" class="drawer-scrim" type="button" aria-label="Close story list" @click="drawerOpen = false"></button>

    <aside class="story-rail" :class="{ 'story-rail--open': drawerOpen }">
      <div class="brand-row">
        <div class="brand-mark"><Shield :size="22" /></div>
        <div>
          <strong>Hero Story</strong>
          <span>Your legend, one choice at a time</span>
        </div>
        <button class="icon-button rail-close" type="button" title="Close stories" @click="drawerOpen = false">
          <X :size="20" />
        </button>
      </div>

      <RouterLink class="new-story-button" to="/" @click="drawerOpen = false">
        <Plus :size="18" />
        <span>Begin new story</span>
      </RouterLink>

      <div class="rail-heading">
        <span>Latest stories</span>
        <BookOpen :size="16" />
      </div>

      <nav class="story-list" aria-label="Latest stories">
        <RouterLink
          v-for="story in sessionStore.sessions"
          :key="story.id"
          class="story-link"
          :class="{ 'story-link--active': story.id === sessionId }"
          :to="`/sessions/${story.id}`"
          @click="drawerOpen = false"
        >
          <span class="story-emblem">{{ story.heroName.slice(0, 1).toUpperCase() }}</span>
          <span class="story-link-copy">
            <strong>{{ story.title }}</strong>
            <small>{{ story.heroName }} · {{ formatDate(story.updatedAt) }}</small>
          </span>
          <span class="status-dot" :title="story.status"></span>
        </RouterLink>
      </nav>

      <div class="rail-footer">
        <button class="account-button" type="button" @click="logout">
          <LogOut :size="18" />
          <span>Sign out</span>
        </button>
      </div>
    </aside>

    <main class="story-stage">
      <header class="story-header">
        <button class="icon-button mobile-menu" type="button" title="Open stories" @click="drawerOpen = true">
          <Menu :size="22" />
        </button>
        <div v-if="workspaceStore.workspace" class="story-heading">
          <span class="story-kicker">{{ workspaceStore.workspace.session.genre }}</span>
          <h1>{{ workspaceStore.workspace.session.title }}</h1>
          <p>
            <span>{{ workspaceStore.workspace.session.heroName }}</span>
            <span aria-hidden="true">·</span>
            <span>{{ workspaceStore.workspace.session.heroArchetype }}</span>
          </p>
        </div>
        <RouterLink class="header-home" to="/" title="Story library">
          <Library :size="20" />
          <span>Stories</span>
        </RouterLink>
      </header>

      <div ref="timelineElement" class="story-scroll">
        <div v-if="workspaceStore.loading" class="workspace-state">
          <LoaderCircle class="spin" :size="24" />
          <p>Opening your story…</p>
        </div>

        <div v-else-if="loadError" class="workspace-state workspace-state--error" role="alert">
          <CircleAlert :size="24" />
          <p>{{ loadError }}</p>
          <button type="button" @click="loadWorkspace">Try again</button>
        </div>

        <div v-else-if="workspaceStore.workspace" class="timeline">
          <section class="episode-marker">
            <span>Episode one</span>
            <strong>{{ workspaceStore.workspace.session.title }}</strong>
          </section>

          <template v-for="turn in workspaceStore.workspace.turns" :key="turn.id">
            <div v-if="turn.sequenceNumber > 1" class="hero-action">
              <span class="hero-action-label"><Zap :size="15" /> Your move</span>
              <p>{{ turn.choiceText }}</p>
            </div>

            <article class="story-turn">
              <header class="turn-header">
                <span>Scene {{ turn.sequenceNumber }}</span>
                <span v-if="turn.location"><MapPin :size="14" /> {{ turn.location }}</span>
              </header>
              <p class="narrative">{{ turn.narrativeText }}</p>

              <div v-if="turn.id === workspaceStore.latestTurn?.id" class="turn-tools">
                <button
                  class="icon-button revision-button"
                  type="button"
                  :disabled="workspaceStore.generating"
                  title="Revise this latest turn"
                  @click="openRevision(turn.choiceText)"
                >
                  <Pencil :size="16" />
                </button>
                <span>Revise latest turn</span>
              </div>

              <form v-if="revisionOpen && turn.id === workspaceStore.latestTurn?.id" class="revision-panel" @submit.prevent="submitRevision(turn.id)">
                <label :for="`revision-${turn.id}`">Replace your previous move</label>
                <textarea
                  :id="`revision-${turn.id}`"
                  v-model="revisionText"
                  rows="2"
                  :disabled="workspaceStore.generating"
                ></textarea>
                <p>The current version will remain in history and this replacement becomes the active path.</p>
                <div class="revision-actions">
                  <button type="button" :disabled="workspaceStore.generating" @click="closeRevision">Cancel</button>
                  <button type="submit" :disabled="!revisionText.trim() || workspaceStore.generating">
                    <LoaderCircle v-if="workspaceStore.generating" class="spin" :size="16" />
                    <span v-else>Replace turn</span>
                  </button>
                </div>
              </form>

              <figure v-if="turn.imageUrl" class="scene-artwork">
                <img :src="turn.imageUrl" :alt="`Artwork for scene ${turn.sequenceNumber}`" />
                <figcaption>{{ turn.sceneSummary }}</figcaption>
              </figure>
              <div v-else-if="isArtworkPending(turn.artworkStatus)" class="artwork-status">
                <ImageIcon :size="18" /> Artwork is developing
              </div>
              <div v-else-if="turn.artworkStatus === 'failed' || turn.artworkStatus === 'poisoned'" class="artwork-status artwork-status--error">
                <ImageOff :size="18" /> Artwork unavailable
              </div>
              <div class="artwork-actions">
                <label class="portrait-toggle">
                  <input v-model="portraitSceneIds[turn.id]" type="checkbox" />
                  <span>Use my private portrait</span>
                </label>
                <button
                  type="button"
                  :disabled="isArtworkPending(turn.artworkStatus) || workspaceStore.artworkSceneId === turn.id"
                  @click="requestArtwork(turn.id, portraitSceneIds[turn.id] === true)"
                >
                  <LoaderCircle v-if="workspaceStore.artworkSceneId === turn.id" class="spin" :size="16" />
                  <ImagePlus v-else :size="16" />
                  <span>{{ turn.imageUrl ? "Generate another image" : "Generate image" }}</span>
                </button>
              </div>
            </article>
          </template>

          <div ref="timelineEnd" class="timeline-end" tabindex="-1"></div>
        </div>
      </div>

      <footer v-if="workspaceStore.workspace" class="composer-zone">
        <div v-if="isEpisodeActive && latestSuggestions.length" class="suggestion-row" aria-label="Suggested actions">
          <button
            v-for="suggestion in latestSuggestions"
            :key="suggestion"
            type="button"
            :disabled="workspaceStore.generating"
            @click="submitAction(suggestion)"
          >
            {{ suggestion }}
          </button>
        </div>
        <form v-if="isEpisodeActive" class="composer" @submit.prevent="submitAction(actionText)">
          <textarea
            v-model="actionText"
            aria-label="What does your hero do?"
            placeholder="What does your hero do?"
            rows="1"
            :disabled="workspaceStore.generating"
            @keydown.enter.exact.prevent="submitAction(actionText)"
          ></textarea>
          <button class="send-button" type="submit" :disabled="!actionText.trim() || workspaceStore.generating" title="Continue story">
            <LoaderCircle v-if="workspaceStore.generating" class="spin" :size="19" />
            <Send v-else :size="19" />
          </button>
        </form>
        <div v-else class="episode-status" :class="`episode-status--${episodeStatus}`">
          <PauseCircle v-if="isEpisodePaused" :size="19" />
          <CircleCheck v-else :size="19" />
          <span v-if="isEpisodePaused">This episode is paused.</span>
          <span v-else>This episode is complete.</span>
          <button v-if="isEpisodePaused" type="button" :disabled="workspaceStore.transitioning" @click="resumeEpisode">
            <Play :size="16" /> Resume episode
          </button>
        </div>
        <div v-if="isEpisodeActive" class="episode-actions">
          <button type="button" :disabled="workspaceStore.generating || workspaceStore.transitioning" @click="pauseEpisode">
            <Pause :size="15" /> Pause episode
          </button>
          <button type="button" :disabled="workspaceStore.generating || workspaceStore.transitioning" @click="concludeEpisode">
            <Flag :size="15" /> Conclude episode
          </button>
        </div>
        <p v-if="generationError" class="composer-error" role="alert">{{ generationError }}</p>
        <p class="composer-note">Your choices shape the story. Suggestions are optional.</p>
      </footer>
    </main>
  </div>
</template>

<script setup lang="ts">
import axios from "axios";
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  BookOpen,
  CircleAlert,
  CircleCheck,
  Flag,
  Image as ImageIcon,
  ImageOff,
  ImagePlus,
  Library,
  LoaderCircle,
  LogOut,
  MapPin,
  Menu,
  Pause,
  PauseCircle,
  Pencil,
  Play,
  Plus,
  Send,
  Shield,
  X,
  Zap
} from "@lucide/vue";
import { useAuthStore } from "../stores/authStore";
import { useSessionStore } from "../stores/sessionStore";
import { useWorkspaceStore } from "../stores/workspaceStore";
import { isArtworkPending } from "../utils/artworkStatus";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const sessionStore = useSessionStore();
const workspaceStore = useWorkspaceStore();
const drawerOpen = ref(false);
const actionText = ref("");
const revisionText = ref("");
const revisionOpen = ref(false);
const loadError = ref("");
const generationError = ref("");
const portraitSceneIds = ref<Record<string, boolean>>({});
const timelineEnd = ref<HTMLElement | null>(null);
const timelineElement = ref<HTMLElement | null>(null);
const sessionId = computed(() => route.params.sessionId as string);
const latestSuggestions = computed(() => workspaceStore.latestTurn?.suggestedActions ?? []);
const episodeStatus = computed(() => workspaceStore.workspace?.session.status ?? "active");
const isEpisodeActive = computed(() => episodeStatus.value === "active");
const isEpisodePaused = computed(() => episodeStatus.value === "paused");
const pollIntervalMs = Number(import.meta.env.VITE_IMAGE_POLL_INTERVAL_MS ?? 3000);
let artworkTimer: number | undefined;

const errorMessage = (error: unknown, fallback: string) =>
  axios.isAxiosError(error) && typeof error.response?.data?.error === "string" ? error.response.data.error : fallback;

const scrollToLatest = async () => {
  await nextTick();
  timelineEnd.value?.scrollIntoView({ behavior: "smooth", block: "end" });
  timelineEnd.value?.focus({ preventScroll: true });
};

const configureArtworkPolling = () => {
  if (artworkTimer !== undefined) window.clearInterval(artworkTimer);
  const hasPendingArtwork = workspaceStore.workspace?.turns.some((turn) => isArtworkPending(turn.artworkStatus));
  if (hasPendingArtwork) {
    artworkTimer = window.setInterval(async () => {
      try {
        await workspaceStore.refresh(sessionId.value);
        if (!workspaceStore.workspace?.turns.some((turn) => isArtworkPending(turn.artworkStatus)) && artworkTimer !== undefined) {
          window.clearInterval(artworkTimer);
          artworkTimer = undefined;
        }
      } catch {
        if (artworkTimer !== undefined) window.clearInterval(artworkTimer);
        artworkTimer = undefined;
      }
    }, pollIntervalMs);
  } else {
    artworkTimer = undefined;
  }
};

const loadWorkspace = async () => {
  loadError.value = "";
  try {
    await Promise.all([workspaceStore.load(sessionId.value), sessionStore.loadSessions()]);
    configureArtworkPolling();
    await scrollToLatest();
  } catch (error) {
    loadError.value = errorMessage(error, "This story could not be opened.");
  }
};

const submitAction = async (value: string) => {
  const choice = value.trim();
  if (!choice || workspaceStore.generating) return;
  generationError.value = "";
  actionText.value = "";
  try {
    await workspaceStore.continueStory(sessionId.value, choice);
    configureArtworkPolling();
    await scrollToLatest();
  } catch (error) {
    actionText.value = choice;
    generationError.value = errorMessage(error, "The story could not continue. Please try again.");
  }
};

const openRevision = (choiceText: string) => {
  revisionText.value = choiceText;
  revisionOpen.value = true;
  generationError.value = "";
};

const closeRevision = () => {
  revisionOpen.value = false;
  revisionText.value = "";
};

const submitRevision = async (sceneId: string) => {
  const choice = revisionText.value.trim();
  if (!choice || workspaceStore.generating) return;
  generationError.value = "";
  try {
    await workspaceStore.reviseLatestTurn(sessionId.value, sceneId, choice);
    closeRevision();
    configureArtworkPolling();
    await scrollToLatest();
  } catch (error) {
    generationError.value = errorMessage(error, "The turn could not be revised. Please try again.");
  }
};

const pauseEpisode = async () => {
  generationError.value = "";
  try {
    await workspaceStore.pauseEpisode(sessionId.value);
    await sessionStore.loadSessions();
  } catch (error) {
    generationError.value = errorMessage(error, "The episode could not be paused. Please try again.");
  }
};

const resumeEpisode = async () => {
  generationError.value = "";
  try {
    await workspaceStore.resumeEpisode(sessionId.value);
    await sessionStore.loadSessions();
  } catch (error) {
    generationError.value = errorMessage(error, "The episode could not be resumed. Please try again.");
  }
};

const concludeEpisode = async () => {
  generationError.value = "";
  try {
    await workspaceStore.concludeEpisode(sessionId.value);
    configureArtworkPolling();
    await sessionStore.loadSessions();
    await scrollToLatest();
  } catch (error) {
    generationError.value = errorMessage(error, "The episode could not be concluded. Please try again.");
  }
};

const requestArtwork = async (sceneId: string, usePortrait: boolean) => {
  generationError.value = "";
  try {
    await workspaceStore.requestArtwork(sessionId.value, sceneId, usePortrait);
    configureArtworkPolling();
  } catch (error) {
    generationError.value = errorMessage(error, "Artwork could not be requested. Please try again.");
  }
};

const logout = async () => {
  await authStore.logout();
  await router.push("/login");
};

const formatDate = (value: string) => new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(new Date(value));

watch(sessionId, () => {
  drawerOpen.value = false;
  void loadWorkspace();
});

onMounted(() => { void loadWorkspace(); });
onBeforeUnmount(() => { if (artworkTimer !== undefined) window.clearInterval(artworkTimer); });
</script>

<style scoped>
.story-workspace {
  min-height: 100dvh;
  display: grid;
  grid-template-columns: 292px minmax(0, 1fr);
  background-color: #f4f1e8;
  background-image:
    linear-gradient(rgba(29, 45, 48, 0.035) 1px, transparent 1px),
    linear-gradient(90deg, rgba(29, 45, 48, 0.035) 1px, transparent 1px);
  background-size: 28px 28px;
  color: #1d2d30;
}

.story-rail {
  position: sticky;
  top: 0;
  height: 100dvh;
  display: grid;
  grid-template-rows: auto auto auto minmax(0, 1fr) auto;
  gap: 20px;
  padding: 24px 18px 18px;
  background: #18272a;
  color: #f7f2e8;
  border-right: 1px solid #31464a;
  z-index: 30;
}

.brand-row { display: grid; grid-template-columns: 42px 1fr auto; align-items: center; gap: 12px; }
.brand-row strong { display: block; font-family: "Avenir Next", "Century Gothic", sans-serif; font-size: 18px; letter-spacing: 0; }
.brand-row span { display: block; margin-top: 2px; color: #9fb1b1; font-size: 11px; line-height: 1.35; }
.brand-mark { width: 42px; aspect-ratio: 1; display: grid; place-items: center; background: #ed6a4f; color: #fff; border-radius: 8px; }

.icon-button { width: 40px; height: 40px; display: grid; place-items: center; border: 1px solid #d3dad6; background: #fff; color: #1d2d30; border-radius: 8px; cursor: pointer; }
.rail-close { display: none; border-color: #40575a; background: transparent; color: #fff; }

.new-story-button { min-height: 46px; display: flex; align-items: center; justify-content: center; gap: 10px; border-radius: 8px; background: #f4f1e8; color: #172629; font-weight: 700; text-decoration: none; }
.new-story-button:hover { background: #fff; }
.rail-heading { display: flex; align-items: center; justify-content: space-between; color: #9fb1b1; padding: 0 6px; font-size: 12px; font-weight: 700; text-transform: uppercase; }
.story-list { overflow-y: auto; display: flex; flex-direction: column; gap: 6px; padding-right: 3px; }
.story-link { min-height: 66px; display: grid; grid-template-columns: 40px minmax(0, 1fr) 8px; align-items: center; gap: 11px; padding: 9px; color: #dce5e2; text-decoration: none; border: 1px solid transparent; border-radius: 8px; }
.story-link:hover { background: #213538; }
.story-link--active { background: #263c3f; border-color: #486064; }
.story-emblem { width: 40px; aspect-ratio: 1; display: grid; place-items: center; background: #d6a94e; color: #1c2c2f; border-radius: 50%; font-family: Charter, Georgia, serif; font-size: 18px; font-weight: 700; }
.story-link-copy { min-width: 0; }
.story-link-copy strong, .story-link-copy small { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.story-link-copy strong { font-size: 14px; }
.story-link-copy small { margin-top: 5px; color: #91a5a5; font-size: 11px; }
.status-dot { width: 7px; aspect-ratio: 1; background: #70ba93; border-radius: 50%; }
.rail-footer { border-top: 1px solid #31464a; padding-top: 14px; }
.account-button { width: 100%; min-height: 42px; display: flex; align-items: center; gap: 10px; border: 0; background: transparent; color: #cbd8d5; cursor: pointer; }

.story-stage { min-width: 0; height: 100dvh; display: grid; grid-template-columns: minmax(0, 1fr); grid-template-rows: auto minmax(0, 1fr) auto; }
.story-header { min-height: 86px; display: grid; grid-template-columns: auto minmax(0, 1fr) auto; align-items: center; gap: 18px; padding: 15px clamp(20px, 4vw, 58px); border-bottom: 1px solid #d8d3c6; background: rgba(244, 241, 232, 0.94); }
.mobile-menu { display: none; }
.story-heading { min-width: 0; }
.story-kicker { color: #b94c3b; font-size: 11px; font-weight: 800; text-transform: uppercase; }
.story-heading h1 { margin: 3px 0 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: Charter, Georgia, serif; font-size: clamp(24px, 3vw, 34px); font-weight: 700; letter-spacing: 0; }
.story-heading p { display: flex; flex-wrap: wrap; gap: 7px; margin: 0; color: #677777; font-size: 12px; }
.header-home { display: flex; align-items: center; gap: 8px; color: #36565a; text-decoration: none; font-weight: 700; font-size: 13px; }

.story-scroll { overflow-y: auto; scroll-behavior: smooth; }
.timeline { width: min(820px, calc(100% - 40px)); margin: 0 auto; padding: 44px 0 56px; }
.episode-marker { display: flex; flex-direction: column; align-items: center; margin-bottom: 38px; text-align: center; }
.episode-marker::after { content: ""; width: 64px; height: 2px; margin-top: 16px; background: #ed6a4f; }
.episode-marker span { color: #b94c3b; font-size: 11px; font-weight: 800; text-transform: uppercase; }
.episode-marker strong { margin-top: 6px; font-family: Charter, Georgia, serif; font-size: 21px; }
.story-turn { padding: 0 0 42px; }
.story-turn + .story-turn { border-top: 1px solid #d8d3c6; padding-top: 38px; }
.turn-header { display: flex; justify-content: space-between; gap: 16px; margin-bottom: 16px; color: #7b8987; font-size: 11px; font-weight: 800; text-transform: uppercase; }
.turn-header span { display: inline-flex; align-items: center; gap: 5px; }
.narrative { margin: 0; white-space: pre-wrap; font-family: Charter, "Iowan Old Style", Georgia, serif; font-size: clamp(18px, 2.1vw, 21px); line-height: 1.78; color: #263638; }
.turn-tools { display: flex; align-items: center; justify-content: flex-end; gap: 8px; margin-top: 18px; color: #677777; font-size: 11px; font-weight: 700; }
.revision-button { width: 32px; height: 32px; border-color: #a8bbb6; background: #eef2ec; color: #285c58; }
.revision-button:disabled { opacity: 0.55; cursor: default; }
.revision-panel { display: grid; gap: 9px; margin-top: 14px; padding: 15px; border: 1px solid #a8bbb6; border-left: 4px solid #23847b; border-radius: 8px; background: #eef2ec; }
.revision-panel label { color: #285c58; font-size: 12px; font-weight: 800; }
.revision-panel textarea { width: 100%; resize: vertical; border: 1px solid #9eadab; border-radius: 6px; padding: 9px; font: 14px/1.45 "Avenir Next", "Century Gothic", sans-serif; color: #1d2d30; }
.revision-panel p { margin: 0; color: #5c706d; font-size: 11px; line-height: 1.45; }
.revision-actions { display: flex; justify-content: flex-end; gap: 8px; }
.revision-actions button { min-height: 34px; display: inline-flex; align-items: center; justify-content: center; gap: 6px; border: 1px solid #789490; border-radius: 6px; padding: 0 11px; background: transparent; color: #285c58; font-weight: 700; cursor: pointer; }
.revision-actions button[type="submit"] { border-color: #1f7770; background: #1f7770; color: #fff; }
.revision-actions button:disabled { opacity: 0.55; cursor: default; }
.hero-action { width: min(620px, 86%); margin: 0 0 34px auto; padding: 15px 18px; background: #dce9e3; border-left: 4px solid #23847b; border-radius: 4px 8px 8px 4px; }
.hero-action-label { display: flex; align-items: center; gap: 6px; color: #236a65; font-size: 10px; font-weight: 800; text-transform: uppercase; }
.hero-action p { margin: 6px 0 0; font-size: 15px; line-height: 1.5; }
.scene-artwork { margin: 28px 0 0; }
.scene-artwork img { display: block; width: 100%; max-height: 520px; object-fit: cover; border-radius: 8px; border: 1px solid #c9c3b7; }
.scene-artwork figcaption { margin-top: 8px; color: #75817f; font-size: 11px; }
.artwork-status { display: flex; align-items: center; gap: 8px; margin-top: 24px; color: #657573; font-size: 12px; }
.artwork-status--error { color: #a3483a; }
.artwork-actions { display: flex; justify-content: flex-end; margin-top: 12px; }
.artwork-actions button { min-height: 32px; display: inline-flex; align-items: center; gap: 6px; border: 1px solid #a8bbb6; border-radius: 6px; padding: 0 10px; background: #eef2ec; color: #285c58; font-size: 12px; font-weight: 700; cursor: pointer; }
.artwork-actions button:disabled { opacity: 0.55; cursor: default; }
.portrait-toggle { display: inline-flex; align-items: center; gap: 6px; margin-right: 10px; color: #5c706d; font-size: 11px; cursor: pointer; }
.timeline-end { height: 1px; }
.workspace-state { min-height: 60vh; display: grid; place-items: center; align-content: center; gap: 10px; color: #60716f; text-align: center; }
.workspace-state p { margin: 0; }
.workspace-state button { border: 0; background: #1f7770; color: #fff; padding: 10px 16px; border-radius: 6px; cursor: pointer; }
.workspace-state--error { color: #a3483a; }

.composer-zone { padding: 12px clamp(20px, 4vw, 58px) 15px; border-top: 1px solid #d8d3c6; background: rgba(247, 244, 237, 0.97); }
.suggestion-row { width: min(820px, 100%); margin: 0 auto 9px; display: flex; gap: 8px; overflow-x: auto; }
.suggestion-row button { flex: 0 0 auto; max-width: 280px; padding: 8px 11px; border: 1px solid #a8bbb6; background: #eef2ec; color: #285c58; border-radius: 6px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; cursor: pointer; }
.composer { width: min(820px, 100%); min-height: 54px; margin: 0 auto; display: grid; grid-template-columns: minmax(0, 1fr) 42px; align-items: end; gap: 10px; padding: 8px 8px 8px 16px; background: #fff; border: 1px solid #9eadab; border-radius: 8px; box-shadow: 0 8px 24px rgba(33, 50, 52, 0.09); }
.composer textarea { width: 100%; max-height: 130px; resize: vertical; border: 0; outline: 0; padding: 9px 0; font: 15px/1.45 "Avenir Next", "Century Gothic", sans-serif; color: #1d2d30; }
.send-button { width: 42px; height: 42px; display: grid; place-items: center; border: 0; background: #ed6a4f; color: #fff; border-radius: 7px; cursor: pointer; }
.send-button:disabled { background: #c8ceca; cursor: default; }
.composer-note, .composer-error { width: min(820px, 100%); margin: 7px auto 0; font-size: 10px; text-align: center; }
.composer-note { color: #7b8885; }
.composer-error { color: #a3483a; }
.episode-actions { width: min(820px, 100%); display: flex; justify-content: flex-end; gap: 8px; margin: 9px auto 0; }
.episode-actions button, .episode-status button { min-height: 32px; display: inline-flex; align-items: center; justify-content: center; gap: 6px; border: 1px solid #a8bbb6; border-radius: 6px; padding: 0 10px; background: #eef2ec; color: #285c58; font-size: 12px; font-weight: 700; cursor: pointer; }
.episode-actions button:last-child { border-color: #b77a41; background: #fff4df; color: #8d5121; }
.episode-actions button:disabled, .episode-status button:disabled { opacity: 0.55; cursor: default; }
.episode-status { width: min(820px, 100%); min-height: 54px; display: flex; align-items: center; justify-content: center; gap: 9px; margin: 0 auto; border: 1px solid #a8bbb6; border-radius: 8px; padding: 10px 14px; background: #eef2ec; color: #285c58; font-size: 13px; font-weight: 700; }
.episode-status--completed { border-color: #d6a94e; background: #fff4df; color: #8d5121; }
.spin { animation: spin 0.9s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
.drawer-scrim { display: none; }

@media (max-width: 820px) {
  .story-workspace { display: block; }
  .story-rail { position: fixed; inset: 0 auto 0 0; width: min(86vw, 320px); transform: translateX(-102%); transition: transform 180ms ease; box-shadow: 16px 0 40px rgba(8, 18, 20, 0.28); }
  .story-rail.story-rail--open { transform: translateX(0); }
  .rail-close { display: grid; }
  .drawer-scrim { display: block; position: fixed; inset: 0; z-index: 25; border: 0; background: rgba(10, 23, 25, 0.52); }
  .story-stage { height: 100dvh; }
  .story-header { min-height: 76px; grid-template-columns: 40px minmax(0, 1fr) auto; padding: 10px 14px; }
  .mobile-menu { display: grid; }
  .story-heading h1 { font-size: 22px; }
  .story-heading p span:last-child, .story-heading p span:nth-last-child(2) { display: none; }
  .header-home span { display: none; }
  .timeline { width: min(100% - 28px, 720px); padding-top: 32px; }
  .narrative { font-size: 18px; line-height: 1.7; }
  .turn-header { flex-direction: column; gap: 5px; }
  .hero-action { width: 90%; }
  .composer-zone { padding: 10px 10px calc(10px + env(safe-area-inset-bottom)); }
  .composer-note { display: none; }
}
</style>
