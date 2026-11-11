# New Generation — Game Design Document

> Living document. Updated in the same commit as any change to how the game works.
> The full original spec is in `/CLAUDE.md`. This file records **what is actually decided and built**.
> Status tags: **[DECIDED]** approved by owner · **[PENDING]** needs owner approval · **[BUILT]** implemented (owner-tested or not, see KNOWN_BUGS).

## 1. Pitch
2D top-down (3/4 view) factory-automation + RTS conquest. Year 4026 traveller crash-lands in Antarctica in 2026, builds a machine civilisation, exterminates humanity. 18+, dark and grim, minimal story. [DECIDED]

## 2. World scale
- Crossing Antarctica on foot: **~10 minutes**. [DECIDED]
- Working math [PENDING final map design, incl. polar distortion]:

| | Value |
|---|---|
| Walk speed | ~8 tiles/s |
| Antarctica across (~4,000 km) | ~4,800 tiles → **1 tile ≈ 830 m** |
| Earth, equator | ~48,000 tiles |
| Earth, pole to pole | ~24,000 tiles |
| Total / land | ~1.15 billion / ~330 million |
| Walk times | Australia ~10 min, Africa N–S ~20 min, Eurasia ~25 min |

### Storage plan [PENDING approval]
1. **Never store generated tiles.** Terrain/resources come from a fixed seed + a hand-painted coarse map, so the game regenerates any chunk identically on demand.
2. **Coarse painted map:** 1 pixel = 8×8 tiles → 6,000 × 3,000 image, 1 byte per pixel (biome id) ≈ 18 MB raw, a few MB as PNG. Painted in the dev World Editor.
3. **Save only changes ("deltas"), per chunk** (32×32 tiles). Untouched chunks cost 0 bytes. Changed chunks store only the differing tiles, compressed.
4. **Buildings are records, not tiles.** A 15×15 building = one entry (type, position, rotation, state) instead of 225 tiles.
5. **Small tiles in memory:** ~2 bytes per tile (terrain id + flags). Only chunks near players are loaded (~a few hundred thousand tiles ≈ under 1 MB).
6. **Far-away human cities run as numbers** (abstract sim), not as tiles.

Rough save size after 100+ hours: tens of MB, mostly buildings/entities — not terrain.
- Milestone 1 uses a **small test map of Antarctica only**. [DECIDED]

## 2b. Terrain tiles (Antarctica)
- Made by Claude (owner request 2026-11-10) with `Tools/terrain/generate_antarctica.py`. [DECIDED]
- Types so far: **snow** (wind-packed, sastrugi ridges), **ice** (wind-polished blue ice, cracks, drifted snow), **rock** (exposed nunatak, snow in hollows).
- Each type = one seamless 512 px texture cut into an 8×8 block of 64 px tiles (`tile_<type>_01..64`). The map places tile `(y mod 8)*8 + (x mod 8) + 1`. Repeats every 8 tiles. [DECIDED — map code comes in step 3]
- Borders between DIFFERENT types (snow→ice etc.) are currently hard edges. How to blend them is [PENDING — will ask during step 3].

## 3. Saving & death
- Normal: manual save max once / 10 min, autosave on quit, multiple slots. Ironman: manual save once / hour, no autosave. [DECIDED]
- Brain upload: if player has zero drones, Brain Core slowly self-builds **one** basic construction drone (no soft-lock). Timer/cost [PENDING]. [DECIDED]

## 4. Technical choices
| Topic | Status |
|---|---|
| Engine: Unity 6 LTS, URP 2D Renderer | [DECIDED] |
| Netcode library | [PENDING — proposal to come before co-op work] |
| Data format (ScriptableObjects vs JSON) | [PENDING — proposal to come before content work] |
| Audio (Unity mixer vs FMOD) | [PENDING] |
| Compile check: owner opens Unity, reports Console errors | [DECIDED] |

## 5. Art
- **64×64 px tiles**, 3/4 top-down, Pixels Per Unit 64, point filtering. [DECIDED — briefly 128 on 2026-11-09, reverted to 64 on 2026-11-10]
- Tool: Pixelorama. Pipeline and naming: see `ART_GUIDE.md`. [DECIDED]
- Import pipeline: PNGs in `Assets/Art/Sprites` (owner), `Assets/Art/Generated` (Claude-made art) and `Assets/Art/_Placeholders` are auto-imported (point filter, uncompressed, 64 PPU). `_<N>f` suffix = N-frame horizontal sheet. A sprite registry maps names → sprites; priority owner > generated > placeholder, with no code changes. Buildings pivot bottom-left, everything else centre. [BUILT — owner-tested 2026-11-09: compiles, placeholders + override + animation slicing work]
- Sprite list & sizes (building height = footprint + 32 px, items 32×32, player 3 directions with mirrored side): [DECIDED 2026-11-09]
- Working-state glow via 2D light: [DECIDED — built later with the building system]
- Drill (Miner MK1) is **3×3** and has an owner-drawn 3-frame working animation (`bld_drill_working_3f`), played at **0.10 s per frame** (frames 1→2→3 loop). **One working animation per output side** (right/left/up/down) — exception to the "one sprite per building" rule. Separate **no-power** sprite `bld_drill_nopower`; a flashing no-power icon will overlay unpowered machines (owner will draw it; overlay code later). [DECIDED 2026-11-10 — playback code comes with machines, step 6]

## 6. Open questions for the owner
- 15 largest faction names · Data Cell tier codenames · tier × branch research chart · story/mystery · infinite research · reparation pricing · terrain art before Earth pre-fill · Earth map design · netcode/data-format choices · Brain Core drone timer/cost.
