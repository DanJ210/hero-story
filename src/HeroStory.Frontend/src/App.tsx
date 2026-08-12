import { FormEvent, useEffect, useMemo, useState } from 'react';
import { createStory, listStories } from './api';
import { Story } from './types';

const initialForm = {
  heroName: '',
  setting: '',
  tone: 'hopeful',
  prompt: ''
};

export function App() {
  const [stories, setStories] = useState<Story[]>([]);
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    listStories().then(setStories).catch((err: Error) => setError(err.message));
  }, []);

  const latestStory = useMemo(() => stories[0], [stories]);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const story = await createStory(form);
      setStories((current) => [story, ...current]);
      setForm(initialForm);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="layout">
      <section className="card">
        <p className="eyebrow">Hero Story MVP</p>
        <h1>Create a bedtime-ready adventure</h1>
        <form onSubmit={onSubmit} className="story-form">
          <label>
            Hero name
            <input value={form.heroName} onChange={(e) => setForm({ ...form, heroName: e.target.value })} required minLength={2} />
          </label>
          <label>
            Setting
            <input value={form.setting} onChange={(e) => setForm({ ...form, setting: e.target.value })} required minLength={2} />
          </label>
          <label>
            Tone
            <input value={form.tone} onChange={(e) => setForm({ ...form, tone: e.target.value })} />
          </label>
          <label>
            Adventure prompt
            <textarea value={form.prompt} onChange={(e) => setForm({ ...form, prompt: e.target.value })} required minLength={8} rows={4} />
          </label>
          <button type="submit" disabled={loading}>{loading ? 'Creating...' : 'Create story'}</button>
          {error ? <p className="error">{error}</p> : null}
        </form>
      </section>

      <section className="card preview">
        <h2>Latest story</h2>
        {latestStory ? (
          <>
            <div className="hero-image">
              <img src={latestStory.coverImageUrl ?? 'https://placehold.co/1200x800?text=Hero+Story'} alt={latestStory.heroName} />
            </div>
            <h3>{latestStory.heroName} in {latestStory.setting}</h3>
            <p className="status">Status: {latestStory.status}</p>
            <ol>
              {latestStory.scenes.map((scene) => (
                <li key={scene.sequence}>
                  <strong>{scene.title}</strong>
                  <p>{scene.narrative}</p>
                </li>
              ))}
            </ol>
          </>
        ) : (
          <p>No stories yet. Create one to preview the generated outline.</p>
        )}
      </section>
    </main>
  );
}
