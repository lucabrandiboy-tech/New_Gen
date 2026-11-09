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
- **128×128 px tiles**, 3/4 top-down, Pixels Per Unit 128, point filtering. [DECIDED 2026-11-09, changed from 64]
  - Note: 15×15 buildings are ~1920 px wide; sprite memory is ~4× the 64 px plan. Fine for now; revisit if VRAM becomes a problem.
- Tool: Pixelorama. Pipeline and naming: see `ART_GUIDE.md`. [DECIDED]
- Import pipeline: PNGs in `Assets/Art/Sprites` (owner) and `Assets/Art/_Placeholders` (generated) are auto-imported (point filter, uncompressed, 128 PPU). `_<N>f` suffix = N-frame horizontal sheet. A sprite registry maps names → sprites; owner art overrides placeholders with no code changes. Buildings pivot bottom-left, everything else centre. [BUILT — not yet owner-tested]
- Sprite list & sizes (incl. building height = footprint + 64 px, items 64×64, player 3 directions with mirrored side): [PENDING approval]
- Working-state glow via 2D light: [DECIDED — built later with the building system]

## 6. Open questions for the owner
- 15 largest faction names · Data Cell tier codenames · tier × branch research chart · story/mystery · infinite research · reparation pricing · terrain art before Earth pre-fill · Earth map design · netcode/data-format choices · Brain Core drone timer/cost.
