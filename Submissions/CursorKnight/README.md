# Cursor Knight

A 2D side-scrolling runner controlled entirely by the mouse cursor. Built for the **POINTER × RUNTIME** game-jam theme.

## Theme interpretation
- **POINTER**: the mouse cursor IS the character's vertical position. Left-click = jump (double-tap for air jump), right-click = slide. No keyboard required. The cursor also controls a custom ink-brush pointer with trail + click-pulse.
- **RUNTIME**: Timeline Scrub — hold **Z** + drag mouse X to rewind the world state up to 2 seconds or fast-forward 0.8s. Post-Processing chromatic aberration, desaturation, grain, and vignette ramp with scrub intensity. Only the world rewinds — the cursor (player's continuous input) stays in real time.

## Tech stack
- Unity **6000.0.72f1**, Built-in render pipeline + Post Processing v2 (Bloom, Vignette, Chromatic Aberration, Color Grading, Grain)
- 100% C#, **~2700 LOC across 22 scripts**
- WebGL build, gzip-compressed (17 MB total over the wire)
- **No scene assets to author** — the entire game spawns at runtime via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. One empty SampleScene.unity exists just so Unity has something to load.
- Procedural audio: 13 WAV SFX + ambient loops synthesized in pure Python stdlib (sine + noise + ADSR)
- Procedural particle systems: dust puffs, screen shake (trauma² Perlin), hurt flash, ground shadow, ambient motion (leaves / bubbles / snowflakes per level)

## Assets & credits (all MIT)
- Character sprites + coin frames + water tiles + some forest scene props: [anshumanpattnaik/unity-2d-forest-assassin-game](https://github.com/anshumanpattnaik/unity-2d-forest-assassin-game) — MIT, Anshuman Pattnaik
- Level 3 "Ice Cave" backdrop + snowy tree / igloo / snowman / ice box / stone / crate obstacles: [striderzz/2D-Platformer-Unity](https://github.com/striderzz/2D-Platformer-Unity) — MIT, Hasan 2023
- Pixel font: **Press Start 2P** — SIL Open Font License (OFL)

## Gameplay
### 3 levels (Forest → Water → Cave boss finale)
- **Level 1 — Forest**: collect **10 coins** in **40 seconds**. Moss, trees, algae, enemies, spinning coins
- **Level 2 — Water**: survive **75 seconds**. Floating logs (slide), waves (jump), whirlpools (fatal), water gaps (fatal)
- **Level 3 — Ice Cave**: finale with **7-HP boss** at 45s. Shadow enemies you click to slash, then boss with 2.4s attack cadence

### Controls
| Input | Action |
|---|---|
| Mouse movement | Samurai follows vertically |
| Left-click | Jump (double-click for mid-air jump) |
| Right-click | Slide |
| **Hold Z + move mouse X** | Timeline scrub (rewind ±2s / fast-forward +0.8s) |
| Space / ↑ | Jump (keyboard fallback) |
| Shift / ↓ | Slide (keyboard fallback) |

## How to build
### WebGL
Open in Unity 6000.0.72f1 → menu **CursorKnight → Build WebGL** → output lands in `Builds/WebGL/`.
The included `Assets/Editor/BuildScript.cs` sets: gzip compression, 512MB memory, WASM linker, MSAA off.

### Desktop
`File → Build Settings → Linux/Windows/Mac → Build`.

## Architecture
```
Assets/Scripts/
  Core/         GameRoot bootstrap, InputManager (cursor NDC + Z-scrub), Score/Combo,
                TimelineScrub (per-obstacle ring buffer), AudioSystem (12-voice pool),
                PostProcessingManager, ScreenShake (trauma²), Hitstop (Celeste pattern),
                DustPuff, FlashFX (hurt flash + full-screen flash), CursorFX, AmbientParticles
  Entities/     Samurai (6-state FSM + cursor-follow + additive-decay jump + frame cycler),
                Obstacle (factory for Cave/Water/Forest kinds with ground-snap + AABB),
                Crystal (animated 16-frame coin), Enemy, Boss, GroundShadow, OscillateY
  Levels/       RunnerLevel (shared scroll + 3-layer parallax + spawn + cluster logic +
                timeline scrub wiring + ambient emitters),
                Level3Forest (L1 UI), Level2Water (L2 UI), Level1Cave (L3 UI + boss)
  UI/           HUD (icon panels + time progress bar + level badge + scrub bar + combo
                popup + animated number tweens), TitleScreen, EditShowcase
  Runtime/      GameRoot (bootstrap + scene switcher + transition + level-tint applier)
```

## Notable polish
- Hitstop on every impact (3 frames boss hit, 4 slash-connect, 8 death, 10 boss kill) — guards against stacking, skipped during scrub
- Double jump with jump buffering (0.12s early-press window)
- Intro drop-in — samurai falls from 4u above ground with dust + shake + thud on landing
- Per-level color grading tint (Forest greener, Water cyan, Cave violet) — unifies mixed art packs
- Timeline Scrub VFX: Post Processing chromatic aberration 0 → 0.9, saturation desaturate on rewind, grain+vignette ramp
- Combo system: multiplier 1→2→3→4→5 at 3/8/16/30 coins, resets on hit, pulsing center display
- Level transition fade + "I · FOREST" / "II · WATER" / "III · ICE CAVE" card slides
- Death desaturate (world tints toward grey-red for 0.6s)
- Parallax value compression (far 45% / mid 72% / near 100% brightness for atmospheric depth)
