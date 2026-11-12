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
- Milestone 1 uses a **small test map of Antarctica only**: ~16 chunks (512 tiles) radius with a wobbly coastline, ocean beyond. Snow plateau with ~18% blue-ice fields and ~8% rock outcrops; resource patches on a jittered 24-tile grid; starter iron/copper/coal/stone patches 16 tiles N/E/S/W of the crash site. [BUILT]

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
| Simulation / view split (below) | [BUILT — pending owner approval of the explanation] |
| Netcode approach: deterministic lockstep (below); library chosen later | [PROPOSED] |
| Data format: JSON in `Assets/StreamingAssets/Content` (below) | [BUILT — pending approval] |
| Audio: Unity Audio Mixer (below) | [PROPOSED, nothing built] |
| Compile check: owner opens Unity, reports Console errors | [DECIDED] |

### 4a. Simulation vs view (built)
The game rules live in `Assets/Scripts/Sim` and have **no Unity code at all**. Unity (`Assets/Scripts/Game`) only draws the state and feeds input in. Why:
- Claude can compile and test the rules outside Unity (`dotnet run --project Tools/simtest`, 200+ checks), so bugs there are found before the owner tests.
- Co-op later: the sim can run the same way on every PC.
- The sim runs at a fixed **30 ticks per second**; all positions are integers (1 tile = 256 units), no floats. Same inputs → bit-identical results on any PC. `SaveSystem.StateHash` proves it (used in tests, will detect desyncs in co-op).

### 4b. Netcode plan (proposed, nothing built)
**Deterministic lockstep:** every player sends only their inputs (move/aim/build commands); every PC runs the identical simulation. This is what Factorio does. It is the natural fit because a factory with 10,000+ entities is far too much state to stream from a server, but inputs are tiny. The groundwork is already in: `PlayerInput` struct, integer math, seeded RNG, state hash. Library choice (Steam relay transport) is decided when co-op starts; it only has to carry small input packets, so almost any works.

### 4c. Data format (built)
Plain **JSON files** in `Assets/StreamingAssets/Content/` (`items.json`, `buildings.json`, `recipes.json`, `techs.json`, `terrain.json`, `resources.json`, `balance.json`, `loc_en.json`). Chosen over ScriptableObjects because: files are readable/diffable in Git, moddable without Unity, testable outside Unity, and `//` comments are allowed. The loader validates and lists problems in the Console. `balance.json` holds every tunable number.

### 4d. Audio (proposed)
**Unity Audio Mixer.** Free, built in, no extra licence or plugin; FMOD's strengths (adaptive music, designer tooling) are not needed for a solo project yet and it adds a licence step before release. Nothing is built; no audio files exist (`LICENSES.md` empty).

## 4e. Rules built so far (Milestone 1) — all [BUILT, not owner-tested] unless marked
- **Power:** wires and tubes are conductors and join each other side by side. Any other building joins a network when its footprint touches a conductor. Buildings do not pass power to each other directly. Each tick satisfaction = supply/demand (capped 100%); machines run at that speed. Pod reactor: 200 kW, decays to 10% over 2 hours. [PENDING approval of these rules]
- **Logistics:** tubes carry items, data-wires carry data cells, 4 per tile, 1 tile per 8 ticks. Items hold when a tube is unpowered; wires never need power. Head-on conveyors do not connect. Arms take from the tile behind and drop on the tile in front (chest/machine/conveyor), 24 ticks per swing. Factorio-style: 1 tile footprint, oversized picture per direction (side 3×3, up 1×2; mirrored for left/down). [DECIDED 2026-11-12]
- **Drill:** 3×3, mines the resource (or ice terrain) under it round-robin, 1 item / 60 ticks, pushes out its facing side into a tube/chest; stalls if blocked. Resources are infinite.
- **Crafters** (smelter/assembler/encoder): one recipe chosen in the building's panel; inputs filtered to that recipe; arms feed/take. No recipes exist yet.
- **Data center L1:** 4×4, accepts data cells, researches the head of the queue (max 10), 1 point/tick; cells consumed proportionally. Heat 2/tick, cooling = terrain cooling × heat rate (snow 1.2, ice 1.4, rock 1.0) so it never overheats in Antarctica; overheats at 1000 heat elsewhere and resumes at 50%.
- **Player:** ~8 tiles/s (snow 90%, ice 110%), cannot walk through buildings except tubes/wires, cannot enter ocean. Temperature 1000 → drains 1/tick (+terrain), warmed near the pod (radius 4) and running smelters. Reaches 6 tiles. Hand mining 60 ticks per ore, 40 per ice. Shooting: 10 dmg, 12 tiles, every 10 ticks. Death = permadeath screen → reload last save.
- **Saving:** binary file per slot (5 slots) in `%AppData%/../LocalLow/<company>/New_Gen/saves`. Manual save every 10 min (F5), Ironman 60 min and no autosave; autosave on quit/return to menu otherwise. Only changed tiles are stored.
- **Buildings are free to place** (no cost) until recipes exist.
- **Chunk streaming:** chunks beyond 5 chunks from the player with no buildings and no changed tiles are unloaded.


## 5. Art
- **64×64 px tiles**, 3/4 top-down, Pixels Per Unit 64, point filtering. [DECIDED — briefly 128 on 2026-11-09, reverted to 64 on 2026-11-10]
- Tool: Pixelorama. Pipeline and naming: see `ART_GUIDE.md`. [DECIDED]
- Import pipeline: PNGs in `Assets/Art/Sprites` (owner), `Assets/Art/Generated` (Claude-made art) and `Assets/Art/_Placeholders` are auto-imported (point filter, uncompressed, 64 PPU). `_<N>f` suffix = N-frame horizontal sheet. A sprite registry maps names → sprites; priority owner > generated > placeholder, with no code changes. Buildings pivot bottom-left, everything else centre. [BUILT — owner-tested 2026-11-09: compiles, placeholders + override + animation slicing work]
- Sprite list & sizes (building height = footprint + 32 px, items 32×32, player 3 directions with mirrored side): [DECIDED 2026-11-09]
- Working-state glow via 2D light: [DECIDED — built later with the building system]
- Drill (Miner MK1) is **3×3** and has an owner-drawn 3-frame working animation (`bld_drill_working_3f`), played at **0.10 s per frame** (frames 1→2→3 loop). **One working animation per output side** (right/left/up/down) — exception to the "one sprite per building" rule. Separate **no-power** sprite `bld_drill_nopower`; a flashing no-power icon will overlay unpowered machines (owner will draw it; overlay code later). [DECIDED 2026-11-10 — playback code comes with machines, step 6]

## 6. Open questions for the owner
- 15 largest faction names · Data Cell tier codenames · tier × branch research chart · story/mystery · infinite research · reparation pricing · terrain art before Earth pre-fill · Earth map design · netcode/data-format choices · Brain Core drone timer/cost.
