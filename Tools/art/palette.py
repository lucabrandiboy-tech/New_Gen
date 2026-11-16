"""Shared colours and drawing helpers for the generated sprites.

House style (same rules the owner's ART_GUIDE sets out):
  * 64 px per tile, 3/4 top-down: you see the roof plus a short front face at the bottom.
  * Light always from the TOP-LEFT: top and left edges catch light, bottom and right fall away.
  * Small palettes, 1 px dark outline, no anti-aliasing.
Tone: dark and grim machine civilisation, cold Antarctic surroundings.
"""
from PIL import Image, ImageDraw

TILE = 64
OVERHANG = 32          # extra height above a building's footprint
WALL = 22              # height of the front face in pixels

def rgb(h):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))

# --- material ramps, darkest first -------------------------------------------------
STEEL  = [rgb(c) for c in ('#161A21', '#242A35', '#3A424F', '#555E6E', '#767F91', '#98A1B2')]
IRON   = [rgb(c) for c in ('#1A1A1E', '#2E3137', '#474B53', '#666B75', '#8A909B', '#AEB4BE')]
COPPER = [rgb(c) for c in ('#2A1409', '#4A2512', '#7A3F1C', '#A85C28', '#C87A3C', '#E0A060')]
COAL   = [rgb(c) for c in ('#0A0B0D', '#141619', '#202329', '#2E323A', '#3F444D', '#545A65')]
STONE  = [rgb(c) for c in ('#1F1E1C', '#35332F', '#4F4C46', '#6D6960', '#8C877C', '#A9A498')]
ICECOL = [rgb(c) for c in ('#13283F', '#1E3C5C', '#2F5C85', '#4A82B0', '#73A9CE', '#A5CDEB')]
SNOW   = [rgb(c) for c in ('#5E6E88', '#8695AD', '#A0B2CF', '#BACAE2', '#DCE6F4', '#F1F6FD')]

HOT      = [rgb(c) for c in ('#3A0D04', '#7A1E06', '#C43B10', '#F2731A', '#FFA637', '#FFD98A')]
DATA     = [rgb(c) for c in ('#042128', '#07434F', '#0B6B7D', '#14A3B8', '#3FD8E0', '#9BF2F5')]
WARN     = [rgb(c) for c in ('#2B0A08', '#5E1512', '#9E241D', '#D63B2C', '#F26A52', '#FFA18C')]
GREENLED = [rgb(c) for c in ('#042A10', '#0A5720', '#128A33', '#1EC24B', '#52E87C', '#A8F7BE')]
SUIT     = [rgb(c) for c in ('#1A1320', '#2E2235', '#48374F', '#66506B', '#8A6F8E', '#B096B2')]

OUTLINE = rgb('#0A0B0E')
BLACK   = (0, 0, 0, 0)

def new(w, h):
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)

def box(d, x0, y0, x1, y1, fill, outline=None):
    """Inclusive rectangle."""
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline)

def hline(d, x0, x1, y, c):
    d.line([x0, y, x1, y], fill=c)

def vline(d, x, y0, y1, c):
    d.line([x, y0, x, y1], fill=c)

def shaded_block(d, x0, y0, x1, y1, ramp, top=4, body=3, side=2, outline=True):
    """A flat slab lit from the top-left: light top/left edge, dark bottom/right edge."""
    box(d, x0, y0, x1, y1, ramp[body])
    hline(d, x0, x1, y0, ramp[top])
    vline(d, x0, y0, y1, ramp[top])
    hline(d, x0, x1, y1, ramp[side])
    vline(d, x1, y0, y1, ramp[side])
    if outline:
        d.rectangle([x0 - 1, y0 - 1, x1 + 1, y1 + 1], outline=OUTLINE)

def building_shell(w_tiles, h_tiles, ramp, wall=WALL):
    """
    The standard 3/4 body every machine starts from.
    Returns (img, draw, roof_rect, wall_rect) where the rects are inclusive pixel bounds.
    The sprite is footprint-wide and footprint+OVERHANG tall; the bottom edge sits on the
    bottom of the footprint, so the extra height leans back into the tile behind.
    """
    W, H = w_tiles * TILE, h_tiles * TILE + OVERHANG
    img, d = new(W, H)
    x0, x1 = 1, W - 2
    y0, y1 = 2, H - 2
    wall_top = y1 - wall
    # front face
    box(d, x0, wall_top, x1, y1, ramp[2])
    hline(d, x0, x1, y1, ramp[1])
    vline(d, x0, wall_top, y1, ramp[3])
    vline(d, x1, wall_top, y1, ramp[1])
    # roof
    box(d, x0, y0, x1, wall_top - 1, ramp[4])
    hline(d, x0, x1, y0, ramp[5])
    vline(d, x0, y0, wall_top - 1, ramp[5])
    vline(d, x1, y0, wall_top - 1, ramp[3])
    hline(d, x0, x1, wall_top - 1, ramp[1])          # roof/wall crease
    d.rectangle([x0 - 1, y0 - 1, x1 + 1, y1 + 1], outline=OUTLINE)
    return img, d, (x0, y0, x1, wall_top - 2), (x0, wall_top, x1, y1)

def panel_lines(d, rect, ramp, step=12, horizontal=True):
    """Faint panel seams across the roof."""
    x0, y0, x1, y1 = rect
    if horizontal:
        for y in range(y0 + step, y1, step):
            hline(d, x0 + 2, x1 - 2, y, ramp[3])
    else:
        for x in range(x0 + step, x1, step):
            vline(d, x, y0 + 2, y1 - 2, ramp[3])

def led(d, x, y, ramp, size=2):
    box(d, x, y, x + size - 1, y + size - 1, ramp[4])
    d.point((x, y), fill=ramp[5])

def rivets(d, rect, ramp, inset=4, step=10):
    x0, y0, x1, y1 = rect
    for x in range(x0 + inset, x1 - inset + 1, step):
        d.point((x, y0 + inset), fill=ramp[5])
        d.point((x, y1 - inset), fill=ramp[1])

def save(img, path):
    img.save(path)
