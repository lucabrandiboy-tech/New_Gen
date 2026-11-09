# Art Guide

You draw everything except roads. This guide teaches the basics and lists every sprite the game needs.
**Golden rules:** 64×64 px per tile · 3/4 top-down view · light from the **top-left**, always · transparent background · PNG.

---

## 1. Tool: Pixelorama (free)
1. Download from **pixelorama.org** (or Steam/itch.io, free). Install.
2. **New canvas:** File → New. Width/height = the sprite's size from the list below (e.g. 64×64 for a 1×1 building). Transparent background ON.
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

**Never** put files in `Assets/Art/_Placeholders/` — that folder is mine and gets regenerated.

---

## 3. Naming convention (strict — the game finds art by name)
All lowercase, underscores, no spaces.

| Kind | Pattern | Example |
|---|---|---|
| Building | `bld_<name>_<state>.png` | `bld_smelter_idle.png` |
| Item | `item_<name>.png` | `item_iron_ore.png` |
| Unit | `unit_<faction>_<name>_<anim>_<N>f.png` | `unit_machine_scoutbot_walk_4f.png` |
| Data cell | `cell_<tier>.png` | `cell_resource.png` |
| Terrain | `tile_<biome>_<variant>.png` | `tile_ice_01.png` |

Building states: `idle`, `damaged`, `destroyed`. (**Working** glow is done with a 2D light — no extra drawing.)

---

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

**Practice exercise:** draw a 64×64 grey metal crate in 3/4 view with top-left lighting. Then a 192×192 version of a "smelter" (box + chimney). Upload them as `practice_crate.png` / `practice_smelter.png` into `Assets/Art/Sprites/` and I'll give feedback.

---

## 5. Sprite list
_Filled in as each feature is designed. Each entry: filename · pixel size · frames · states · what it must communicate._

| File | Size | Frames | States | Notes |
|---|---|---|---|---|
| _(added with Milestone 1, step 2)_ | | | | |
