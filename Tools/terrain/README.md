# Terrain generator

`generate_antarctica.py` makes the Antarctica ground tiles in `Assets/Art/Generated/`.

- Each type (snow, ice, rock) is one seamless 512×512 texture cut into an **8×8 block** of
  64×64 tiles: `tile_<type>_01.png` … `tile_<type>_64.png`, numbered row by row from the top-left.
- The map must place tile number `(y mod 8) * 8 + (x mod 8) + 1` at world tile (x, y)
  (y counted downward on the block). Then neighbouring tiles continue each other and the pattern
  only repeats every 8 tiles. Placing them randomly WILL show seams.
- Everything is periodic, so the 8×8 block also tiles with itself.
- Light from the top-left, cool palettes shared between types.

Regenerate (needs Python 3 + numpy + Pillow):

    python3 Tools/terrain/generate_antarctica.py Assets/Art/Generated --preview <some_folder>

Seeds are fixed, so the output is identical every run unless the script changes.
