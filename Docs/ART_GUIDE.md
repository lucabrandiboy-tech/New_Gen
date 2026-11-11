# Art Guide

You draw everything except roads. This guide teaches the basics and lists every sprite the game needs.
**Golden rules:** 64×64 px per tile · 3/4 top-down view · light from the **top-left**, always · transparent background · PNG.

---

## 1. Tool: Pixelorama (free)
1. Download from **pixelorama.org** (or Steam/itch.io, free). Install.
2. **New canvas:** File → New. Width/height = the sprite's size from the list below (e.g. 64×64 for a 1×1 tile). Transparent background ON.
3. **Grid:** Edit → Preferences → Canvas → Grid: 64×64 (tile grid). Add a second "pixel grid" at 1×1 for zoomed-in work. Toggle with View → Show Grid.
4. **Zoom** in a lot (800%+). Use the **Pencil** tool (1 px), never soft brushes. Turn anti-aliasing OFF.
5. **Palette:** keep it small (≈32 colours for the whole game). We'll create a shared palette file later so all your art matches.
6. **Save** your work file as `.pxo` (stays on your PC, not uploaded).
7. **Export:** File → Export → PNG. Scale **1×** (never upscale). Transparent background. Use the exact filename from the list.

### Animations (sprite sheets)
Draw each frame in Pixelorama's timeline. Export → **Spritesheet**, **1 row**, frames left→right, no padding. Example: 4 frames of 64×64 → one 256×64 PNG. The filename says the frame count: `unit_player_walk_4f.png`.

---

## 2. Uploading art
One-time setup:
1. Install **GitHub Desktop** (desktop.github.com), sign in, **Clone** `New_Gen`.
2. Install **Git LFS** (git-lfs.com), run the installer once. The repo already tells LFS to handle PNGs.

Every time:
1. Export PNG with the exact filename into `New_Gen/Assets/Art/Sprites/`.
2. GitHub Desktop → write a summary ("smelter art") → **Commit to main** → **Push origin**.
3. Next session I pull it. The game uses your file instead of the placeholder automatically.

**Never** put files in `Assets/Art/_Placeholders/` or `Assets/Art/Generated/` — those folders are mine. `_Placeholders` = flat colour stand-ins; `Generated` = finished art I made (terrain).

---

## 3. Naming convention (strict — the game finds art by name)
All lowercase, underscores, no spaces, **PNG only**.

| Kind | Pattern | Example |
|---|---|---|
| Building | `bld_<name>_<state>.png` | `bld_smelter_idle.png` |
| Item | `item_<name>.png` | `item_iron_ore.png` |
| Unit | `unit_<who>_<anim>_<direction>[_<N>f].png` | `unit_player_walk_down_4f.png` |
| Data cell | `cell_<tier>.png` | `cell_resource.png` |
| Terrain | `tile_<biome>_<variant>.png` | `tile_ice_01.png` |
| Resource patch | `res_<resource>_<variant>.png` | `res_iron_01.png` |

- Building states: `idle`, `damaged`, `destroyed`. (**Working** glow is done with a 2D light — no extra drawing.)
- **Animations:** end the name with `_<N>f` where N = number of frames, e.g. `_4f`. The image must be exactly N frames wide in one row. The game slices it automatically.
- **Tubes/wires orientation:** draw `bld_tube_straight` / `bld_wire_straight` **flowing from the bottom to the top** of the tile. Draw `bld_tube_corner` / `bld_wire_corner` **entering from the left edge and leaving through the top**. The game rotates/mirrors them for every other case. `bld_arm` points **up** (it drops items at the top).
- If your file has the same name as a placeholder, **yours replaces it automatically**. You can put files in subfolders inside `Sprites/` if you like — only the file name matters.

### What the import pipeline does to your files
Nothing is changed in the PNG itself. Unity just imports it as: pixel-perfect (no blur), no compression, 64 px = 1 tile.
- **Buildings** (`bld_...`) are anchored at their **bottom-left corner** = the bottom-left tile of their footprint. Anything taller than the footprint sticks up into the tiles behind it (that's the 3/4 roof).
- Everything else is anchored at its **centre**.

## 4. Drawing in 3/4 top-down
Imagine looking down at about 45°. You see the **roof** (top) and the **front (south) wall**, nothing else.

```
  +----------------+   <- roof: lightest, takes most of the sprite
  |   ROOF         |
  |                |
  +----------------+
  |  FRONT WALL    |   <- front face: darker, ~1/3 of the height
  +----------------+   <- bottom edge sits on the tile's bottom
```

- **Footprint:** a building of N×N tiles = N×64 px wide. It can be **taller** than N×64 (roof/chimneys overhang upward into the tile behind). Bottom edge always lines up with the bottom of its footprint.
- **Wall height:** front wall ≈ **1/3** of the visible height for normal buildings; tall structures (towers, data centers) can go to 1/2.
- **Light from top-left:** roof = base colour, left/top edges highlighted 1 shade lighter, front wall 1–2 shades darker, right side darkest. **Shadow** falls to the bottom-right (a simple dark shape, ~30% opacity is done by the game later — don't draw shadows on the ground for now).
- **Outline:** 1 px dark outline (not pure black — darkest palette colour) around the whole object. Makes it readable on any terrain.
- **Size scaling:**
  - **1×1 (64 px):** one clear shape, 2–3 details max. Must read at a glance.
  - **3×3 (192 px):** main body + 2–3 distinct parts (chimney, tank, panel).
  - **15×15 (960 px):** a complex. Treat it as several smaller buildings joined on one base slab. Repeat textures (vents, panels) to save time.
- **Readability test:** zoom out to 100%. If you can't tell what it is in 1 second, simplify.

**Practice exercise:** draw a 64×64 grey metal crate in 3/4 view with top-left lighting. Then a 128×160 (2×2 building) "smelter" (box + chimney). Upload them as `practice_crate.png` / `practice_smelter.png` into `Assets/Art/Sprites/` and I'll give feedback.

---

## 5. Sprite list — APPROVED 2026-11-09
These sizes are approved and match the placeholders. Light always from the top-left.

**Building height rule (approved):** sprite width = footprint × 64; sprite height = footprint × 64 **+ 32 px** for the roof/upper wall in 3/4 view.

| File | Size (px) | Frames | Notes / what it must show at a glance |
|---|---|---|---|
| `tile_snow_01`…`_64`, `tile_ice_01`…`_64`, `tile_rock_01`…`_64` | 64×64 | 1 | **Made by Claude** (see below) — you don't need to draw these. |
| `res_iron_01`, `res_copper_01`, `res_coal_01`, `res_stone_01` | 64×64 | 1 | Ore drawn **on top of** terrain (transparent around it). Colour must say which ore instantly. |
| `item_iron_ore`, `item_copper_ore`, `item_coal`, `item_stone`, `item_ice`, `item_iron_plate`, `item_copper_plate` | 32×32 | 1 | Inventory icons, also shown flying through tubes. Bold silhouette. |
| `cell_resource` | 32×32 | 1 | Tier-1 Data Cell. Should look like tech, not a rock. |
| `unit_player_idle_down` / `_up` / `_side` | 64×64 | 1 | Player in cold suit. `side` faces right; the game mirrors it for left. |
| `unit_player_walk_down_4f` / `_up_4f` / `_side_4f` | 256×64 | 4 | Walk cycle, 4 frames of 64×64. |
| `bld_crashpod_<state>` | 192×224 | 1 | 3×3. Crashed time pod with dying reactor. The player's start point. |
| `bld_drill_<state>` | 192×224 | 1 | **3×3**. Mining drill ("Miner MK1"). |
| `bld_drill_idle` | 192×224 | 1 | Default look = frame 1 of the animation (powered, not running). |
| `bld_drill_working_right_3f`, `_up_3f`, `_down_3f` (576×224, 3 frames); `bld_drill_working_left_5f` (960×224, 5 frames) | — | 3 / 5 | Working animation, **one per output side** (the side the ore comes out). Frames 1, 2, 3 side by side (192×224 each), looped, **0.10 s per frame**. |
| `bld_drill_nopower` | 192×224 | 1 | Drill with no power. (Any building can have a `_nopower` sprite; if missing the idle sprite is shown dimmed.) |
| `icon_nopower`, `icon_missing_input`, `icon_overheat` | 24×24 | 1 | Small icons that **flash above a machine** when it has no power / is missing inputs / overheated. Transparent background. Must read at a glance. |
| `fx_bullet` | 12×6 | 1 | The player's bullet, drawn pointing right. |
| `tile_ocean_01` | 64×64 | 1 | Dark sea water past the coast (Claude will generate this later like the other terrain; placeholder for now). |
| `bld_smelter_<state>` | 128×160 | 1 | 2×2. Furnace, should look hot. |
| `bld_assembler_<state>` | 192×224 | 1 | 3×3. Assembler. |
| `bld_encoder_<state>` | 128×160 | 1 | 2×2. Data Cell encoder. |
| `bld_datacenter_l1_<state>` | 256×288 | 1 | 4×4. Level 1 Data Center. Important target — should look valuable. |
| `bld_chest_<state>` | 64×96 | 1 | 1×1. Storage chest. |
| `bld_tube_straight`, `bld_tube_corner` | 64×64 | 1 | Flat, seen from above. Straight runs left↔right; corner connects left↔down. Game rotates them. |
| `bld_wire_straight`, `bld_wire_corner` | 64×64 | 1 | Same as tubes, for data-wires. |
| `bld_arm` | 64×64 | 1 | Arm, pointing up. Game rotates it. (Animation later.) |

`<state>` = `idle`, `damaged`, `destroyed` → 3 files per building.

---

## 6. Terrain (made by Claude)
You asked me to make the Antarctica terrain, so it lives in `Assets/Art/Generated/` and is made by `Tools/terrain/generate_antarctica.py`.

- Each type is **one big 512×512 picture cut into an 8×8 block** of 64 px tiles, numbered 01–64 row by row from the top-left. The map lays them out in that order, so edges always line up and the pattern only repeats every 8 tiles.
- **If you want to draw terrain yourself:** a single replacement tile (e.g. your own `tile_snow_05`) won't line up with its neighbours. Draw a whole new type instead (tell me the name first, e.g. `tile_crevasse`), or ask me to change the generator.
- Colours to match when you draw things that sit on the ground:
  - Snow: `#A0B2CF` `#BACAE2` `#CFDCEE` `#E1EAF7` `#F1F6FD`
  - Blue ice: `#3A68A0` `#4E82B8` `#659BCC` `#82B4DD` `#A5CDEB`
  - Rock: `#222129` `#32313A` `#44424C` `#595660` `#726E77`

---

## 7. "Check" — still to draw before Milestone 1 is finished
Say **"Check"** and Claude lists what's left (compares this list with what's in `Assets/Art/Sprites/`). Terrain is done by Claude.

**Player** (64×64 per frame)
- `unit_player_idle_down`, `unit_player_idle_up`, `unit_player_idle_side`
- `unit_player_walk_down_4f`, `unit_player_walk_up_4f`, `unit_player_walk_side_4f`

**Crash pod** (3×3 → 192×224): `bld_crashpod_idle`, `_damaged`, `_destroyed`

**Drill / Miner MK1** (3×3 → 192×224)
- `bld_drill_idle`, `bld_drill_nopower`, `bld_drill_damaged`, `bld_drill_destroyed`
- `bld_drill_working_right_3f`, `_up_3f`, `_down_3f` (576×224 each), `bld_drill_working_left_5f` (960×224)

**Smelter** (2×2 → 128×160): `bld_smelter_idle`, `_damaged`, `_destroyed`
**Assembler** (3×3 → 192×224): `bld_assembler_idle`, `_damaged`, `_destroyed`
**Data Cell encoder** (2×2 → 128×160): `bld_encoder_idle`, `_damaged`, `_destroyed`
**Data Center L1** (4×4 → 256×288): `bld_datacenter_l1_idle`, `_damaged`, `_destroyed`
**Chest** (1×1 → 64×96): `bld_chest_idle`, `_damaged`, `_destroyed`

**Logistics** (64×64, flat): `bld_tube_straight`, `bld_tube_corner`, `bld_wire_straight`, `bld_wire_corner`, `bld_arm`

**Resource patches** (64×64, transparent around the ore): `res_iron_01`, `res_copper_01`, `res_coal_01`, `res_stone_01`

**Items** (32×32): `item_iron_ore`, `item_copper_ore`, `item_coal`, `item_stone`, `item_ice`, `item_iron_plate`, `item_copper_plate`
**Data Cell** (32×32): `cell_resource`

**Icons** (24×24, flash above machines): `icon_nopower`, `icon_missing_input`, `icon_overheat`
**Bullet** (12×6): `fx_bullet`

