# Audio mix

The goal is a mix where combat reads clearly without ever feeling sharp or
tiring after a few waves. The old setup failed on four fronts:

1. **No headroom.** Every clip is imported with *Normalize* on, so every sound
   peaked at 0 dBFS, and every `Sound.volume` was `1`. Two or three overlapping
   hits summed past full scale and hard-clipped — that clipping is the harsh
   "tearing" you hear during a busy wave.
2. **No overlap.** `AudioManager` created exactly one `AudioSource` per sound and
   called `Play()`. A second splat or throw restarted the same source, cutting
   the first one off mid-transient. Rapid fire became a stutter.
3. **No variation.** Identical pitch on every repetition turns a sound into a
   machine gun and fatigues the ear within seconds.
4. **Wrong balance.** Music sat at `0.2` while effects sat at `1.0`, so effects
   were roughly five times louder than the bed they were supposed to sit on.

## What the manager does now

| Behaviour | Why |
| --- | --- |
| Per-sound voice pool (`voices`, 1–8) | Overlapping one-shots finish instead of cutting each other off |
| Global `masterVolume` (0.9) + per-clip `volume` | Keeps the sum of simultaneous hits below full scale |
| `minInterval` per sound | Same-frame stacking is collapsed into one hit instead of a burst |
| `pitchJitter` (±2–8%) | Breaks up repetition without sounding like a different effect |
| Music fade-in + `duckDuration` stingers | Music enters gently and steps aside for big moments |
| `priority` per sound | Frequent combat sounds are culled before rare, important ones |

Music is faded to `musicVolume` (0.35) times its own `Sound.volume` (1.0), so the
bed peaks about 10 dB under a typical combat hit and ~20 dB under the stingers.
Stingers (`Explosion`, `FinalWaveSound`) dip it to 45% for their duration and
bring it back over half a second, using unscaled time so this still works while
the game is paused at level clear.

## Current levels

| Sound | Volume | Voices | Min interval | Pitch jitter |
| --- | ---: | ---: | ---: | ---: |
| `Bg` (music) | 1.00 (× 0.35) | 1 | – | – |
| `Plant` | 0.55 | 2 | 40 ms | ±5% |
| `Shovel` | 0.50 | 2 | 30 ms | ±3% |
| `Buzzer` | 0.35 | 1 | 250 ms | – |
| `SeedLift` | 0.38 | 2 | 50 ms | ±4% |
| `Points` | 0.42 | 3 | 50 ms | ±5% |
| `Falling1/2` | 0.45 | 2 | 80 ms | ±5% |
| `ZombiesEntering` | 0.50 | 1 | 500 ms | – |
| `Throw1/2` | 0.30 | 4 | 50 ms | ±7% |
| `Splat1/2/3` | 0.28 | 4 | 50 ms | ±8% |
| `Squash1/2` | 0.45 | 2 | 100 ms | ±5% |
| `DirtRise` | 0.45 | 2 | 50 ms | ±4% |
| `Explosion` | 0.60 | 2 | 80 ms | ±2% |
| `FinalWaveSound` | 0.50 | 1 | 500 ms | – |

Volumes are intentionally conservative. Six to ten decibels of headroom is what
stops a wave of overlapping splats from turning into distortion; loudness should
come from the player's system volume, not from the mix.

## Tuning it

Everything lives on the `AudioManager` object in `Game.unity` (and the matching
prefab under `Assets/Scripts/Tools/`):

- **Whole game too loud / too quiet** — `masterVolume`.
- **Music too forward** — `musicVolume` (default `0.35`); effects are unaffected.
- **A single effect is piercing** — lower that entry's `volume`. Buzzer, throw and
  splat are the usual suspects because they repeat most often.
- **Combat still stutters** — raise `voices` on `Throw`/`Splat`, or lower their
  `minInterval` if hits are being swallowed.
- **Something sounds robotic** — raise `pitchJitter` on that entry (stay ≤ 0.12
  or it starts sounding detuned).
- **Stinger drowns the music** — raise `stingerDuckAmount` toward 1 (no dip), or
  lower the entry's `duckDuration`.
- **Music cut off abruptly at level clear** — call `AudioManager.Instance.StopMusic()`
  where the state changes; it fades over 0.8 s.

`SetMasterVolume` and `SetMusicVolume` exist for a future settings menu and
update every live source immediately.

## Listening checklist

Play one level with headphones and confirm:

- [ ] No crackle or tearing when several bots fire at once.
- [ ] Throws and splats overlap smoothly instead of stuttering.
- [ ] Repeated splats vary enough to not feel mechanical.
- [ ] Music is audible under combat but never competes with it.
- [ ] The final-wave stinger is loud and clear, then the music returns smoothly.
- [ ] The buzzer still communicates "not enough energy" without stinging.

## Optional next step

Every clip is imported with *Normalize* enabled, which flattens the intended
level differences between clips. Turning it off per clip (Inspector → Audio
Import Settings) gives each sound its original dynamics, at which point the
per-clip `volume` values above should be re-checked by ear.
