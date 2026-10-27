# Balance and difficulty curve

The target is a run that stays tense from the first wave to the last. Two
failure modes are explicitly designed out:

- **Plant snowball** – the player banks so much photon energy that the board
  becomes a spectator sport and later waves cannot matter.
- **Alien snowball** – one bad wave snowballs into an unrecoverable loss
  because the player is never given a chance to rebuild.

The curve is built in four layers. Only the last one adapts at runtime, and it
is deliberately bounded.

## 1. Economy: slower income, faster payback

| Lever | Before | Now (Levels 1 / 2 / 3) |
| --- | ---: | ---: |
| Starting photons | 150 / 175 / 200 | **200 / 225 / 250** |
| Passive photon interval | 8 / 7 / 6 s | **11 / 10 / 9 s** |
| SolarCore interval | 14 s | **24 s** |
| SolarCore first photon | 14 s | **8 s** |
| Photon value | 25 | 25 |

A SolarCore still costs 50, so two photons (8 s + 24 s) pay it back. The
shorter first tick keeps a freshly planted core from feeling dead, while the
longer steady interval stops a wall of cores from printing an unlimited bank.
The higher starting bank gives every level a real opening instead of forcing a
single viable build order.

The shovel returns a **partial** salvage: one photon (25) for a 50-cost bot and
two (50) for a 100-cost bot. Full refunds were removed because they let a
damaged ShieldCore be replanted for free, which trivialised every blocker.

## 2. Authored ramp: each level gets harder on its own

Every level asset now carries start/end multipliers that interpolate across its
waves (`zombieHealthStart/End`, `zombieSpeedStart/End`,
`zombieDamageStart/End`). This keeps the difficulty curve data-driven: designers
can make Level 3's last wave hit 1.6x health without touching code.

| Level | Waves | Aliens | Health ramp | Speed ramp | Damage ramp | Max wave window |
| --- | ---: | ---: | --- | --- | --- | ---: |
| Tutorial | 2 | 3 | 1.0 → 1.0 | 1.0 → 1.0 | 1.0 → 1.0 | 30 s |
| Level 1 | 6 | 30 | 1.0 → 1.30 | 1.0 → 1.06 | 1.0 → 1.05 | 26 s |
| Level 2 | 6 | 42 | 1.0 → 1.45 | 1.0 → 1.08 | 1.0 → 1.10 | 24 s |
| Level 3 | 7 | 59 | 1.0 → 1.60 | 1.0 → 1.12 | 1.0 → 1.15 | 22 s |

## 3. Bounded wave windows: no free farming, no infinite stall

The spawner no longer waits forever for the last straggler. Each wave has a
`maxWaveWindow`:

- Clear the wave quickly → the next wave's gap starts immediately.
- Leave a straggler alive → the next wave starts after the window anyway.

This is the main anti-snowball tool. A dominant board cannot park one slow
alien at the far end of the lawn and farm passive photons behind it, while a
board that is genuinely struggling still gets its full recovery gap.

## 4. `DifficultyDirector`: a small, bounded rubber band

`Assets/Scripts/DifficultyDirector.cs` runs during normal levels only (never in
the tutorial) and only after the first wave has been live for 35 seconds. It
samples the board four times a second and derives an `Advantage` in `[-1, +1]`
from:

- photon bank,
- number of PulseBots (attackers),
- number of SolarCores (economy),
- recent bot losses (weighted relief, decaying over ~12 s).

The value is smoothed (`reactionSpeed`), so the game never snaps. It then
trims only the extremes:

| Output | Struggling (-1) | On curve (0) | Dominating (+1) |
| --- | ---: | ---: | ---: |
| Alien health | ×0.88 | ×1.00 | ×1.12 |
| Passive photon interval | ×0.85 | ×1.00 | ×1.35 |
| Wave gap | ×1.35 | ×1.00 | ×0.80 |
| Wave window | ×1.30 | ×1.00 | ×0.85 |
| Simultaneous-alien cap | −4 | base | +3 |
| Bonus reinforcements | 0 | 0 | up to 4 (huge wave) / 2 |

The director never changes the authored wave list, never removes enemies and
never speeds enemies up. If the player is collapsing, the worst it can do is
soften health by 12 %, widen the gaps and add a little passive income. If the
player is dominating, the worst it can do is add 12 % health, four extra aliens
and slightly shorter pacing.

## Tuning checklist

All of the frequent knobs are in data, not code:

- **Too easy overall:** raise `zombieHealthEnd`, lower `sunSpawnInterval`, or
  lower `startingSun` in the level asset.
- **Too hard overall:** the reverse. Prefer health/ramp changes over cutting
  wave counts; the wave shape is what makes the level feel authored.
- **Waves feel rushed:** raise `maxWaveWindow` and `delayBeforeWave`.
- **Player can idle between waves:** lower `maxWaveWindow`.
- **Director too intrusive:** lower `reactionSpeed` or narrow the multipliers in
  `DifficultyDirector`. The director is created automatically by `GameManager`;
  add the component to `Game.unity` if you want its serialized values exposed in
  the Inspector (the auto-creation then reuses that instance).
- **Economy feels dead:** shorten `firstGenerationDelay` or the SolarCore
  `generationInterval` in `SunFlower.prefab`.

## Verifying a change

Unity playtests are the source of truth. A quick pass:

1. Play Level 1 with a "greedy" opener (two SolarCores + one PulseBot) and
   confirm the first two waves are survivable but not free.
2. Play a losing board (let a wave through) and confirm the next wave arrives
   with a visibly longer gap.
3. Play a dominant board (several cores, full lane coverage) and confirm the
   next wave arrives promptly and the director's bonus aliens appear.
4. Retry and confirm the shovel no longer returns a full refund.
