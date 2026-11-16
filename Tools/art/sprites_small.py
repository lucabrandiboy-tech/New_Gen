"""Items, data cells, ore patches, HUD icons, bullet, and the shared damaged/destroyed art."""
import random
from palette import *

# ---------------------------------------------------------------- items (32x32)
def _chunk(d, cx, cy, r, ramp):
    """A faceted rock lit from the top-left."""
    for y in range(-r, r + 1):
        half = int((r * r - y * y) ** 0.5)
        for x in range(-half, half + 1):
            t = (x - y)                      # top-left to bottom-right gradient
            idx = 4 if t < -r // 2 else 3 if t < r // 3 else 2
            d.point((cx + x, cy + y), fill=ramp[idx])
    # rim light + outline
    for y in range(-r, r + 1):
        half = int((r * r - y * y) ** 0.5)
        d.point((cx - half, cy + y), fill=ramp[5] if y < 0 else OUTLINE)
        d.point((cx + half, cy + y), fill=OUTLINE)
    d.point((cx, cy - r), fill=ramp[5])
    d.point((cx, cy + r), fill=OUTLINE)

def ore_item(ramp):
    img, d = new(32, 32)
    _chunk(d, 12, 14, 7, ramp)
    _chunk(d, 21, 20, 5, ramp)
    _chunk(d, 20, 10, 4, ramp)
    return img

def plate_item(ramp):
    img, d = new(32, 32)
    for i, oy in enumerate((20, 15, 10)):
        shaded_block(d, 5 + i, oy, 26 - i, oy + 5, ramp, top=5, body=4, side=2)
    return img

def ice_item():
    img, d = new(32, 32)
    pts = [(16, 4), (25, 13), (21, 27), (11, 27), (6, 13)]
    d.polygon(pts, fill=ICECOL[3], outline=OUTLINE)
    d.polygon([(16, 6), (16, 25), (9, 14)], fill=ICECOL[4])
    d.line([(16, 6), (23, 13)], fill=ICECOL[5])
    d.line([(16, 6), (16, 25)], fill=ICECOL[5])
    return img

def data_cell():
    img, d = new(32, 32)
    shaded_block(d, 7, 5, 24, 26, STEEL, top=4, body=2, side=1)
    box(d, 10, 9, 21, 22, DATA[1])
    for y in range(10, 22, 3):
        hline(d, 11, 20, y, DATA[3])
    box(d, 13, 12, 18, 19, DATA[4])
    box(d, 14, 13, 17, 15, DATA[5])
    hline(d, 9, 22, 7, STEEL[5])
    return img

# ---------------------------------------------------------------- ore patches (64x64)
def ore_patch(ramp, seed, count=7):
    """Rocks on a transparent background so the terrain shows between them."""
    img, d = new(64, 64)
    rnd = random.Random(seed)
    spots = []
    for _ in range(count * 6):
        if len(spots) >= count:
            break
        x, y, r = rnd.randint(8, 55), rnd.randint(8, 55), rnd.randint(4, 8)
        if all((x - sx) ** 2 + (y - sy) ** 2 > (r + sr) ** 2 for sx, sy, sr in spots):
            spots.append((x, y, r))
    for x, y, r in sorted(spots, key=lambda s: s[1]):
        d.ellipse([x - r, y - r + 2, x + r, y + r + 2], fill=(0, 0, 0, 70))   # contact shadow
        _chunk(d, x, y, r, ramp)
    return img

# ---------------------------------------------------------------- HUD icons (24x24)
def icon_nopower():
    img, d = new(24, 24)
    bolt = [(13, 2), (6, 13), (11, 13), (9, 22), (18, 10), (13, 10)]
    d.polygon([(x + 1, y + 1) for x, y in bolt], fill=OUTLINE)
    d.polygon(bolt, fill=WARN[4])
    d.polygon([(13, 3), (8, 12), (12, 12)], fill=WARN[5])
    return img

def icon_missing_input():
    img, d = new(24, 24)
    d.rectangle([3, 12, 20, 21], outline=OUTLINE, fill=STEEL[2])
    hline(d, 4, 19, 12, STEEL[4])
    arrow = [(11, 1), (11, 7), (7, 7), (12, 12), (17, 7), (13, 7), (13, 1)]
    d.polygon([(x + 1, y + 1) for x, y in arrow], fill=OUTLINE)
    d.polygon(arrow, fill=WARN[4])
    return img

def icon_overheat():
    img, d = new(24, 24)
    flame = [(12, 1), (17, 9), (15, 11), (19, 17), (12, 23), (5, 17), (8, 10), (10, 12)]
    d.polygon([(x + 1, y + 1) for x, y in flame], fill=OUTLINE)
    d.polygon(flame, fill=HOT[3])
    d.polygon([(12, 7), (15, 15), (12, 20), (9, 15)], fill=HOT[4])
    d.polygon([(12, 12), (13, 16), (12, 19), (11, 16)], fill=HOT[5])
    return img

# ---------------------------------------------------------------- misc
def bullet():
    img, d = new(12, 6)
    box(d, 0, 2, 7, 3, HOT[4])
    box(d, 6, 1, 10, 4, HOT[5])
    d.point((11, 2), fill=HOT[5]); d.point((11, 3), fill=HOT[5])
    return img

def damaged_overlay():
    """Laid OVER a hurt building: scorch, bold cracks and a few sparks, on transparency."""
    img, d = new(64, 64)
    rnd = random.Random(99)
    # scorch blotches
    for _ in range(3):
        x, y, r = rnd.randint(14, 50), rnd.randint(14, 50), rnd.randint(7, 13)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(14, 11, 10, 95))
    # cracks: a dark line with a light edge so they read on any colour
    for _ in range(4):
        x, y = rnd.randint(10, 53), rnd.randint(10, 53)
        ang = rnd.random() * 6.28
        for _ in range(rnd.randint(6, 11)):
            ang += rnd.uniform(-0.8, 0.8)
            nx = max(2, min(61, x + int(5 * __import__('math').cos(ang))))
            ny = max(2, min(61, y + int(5 * __import__('math').sin(ang))))
            d.line([x, y, nx, ny], fill=(8, 7, 9, 235))
            d.line([x + 1, y + 1, nx + 1, ny + 1], fill=(170, 170, 180, 90))
            x, y = nx, ny
    # a few hot sparks
    for _ in range(6):
        x, y = rnd.randint(6, 57), rnd.randint(6, 57)
        d.point((x, y), fill=HOT[4] + (240,))
        d.point((x + 1, y + 1), fill=HOT[3] + (160,))
    return img

def destroyed_tile():
    """REPLACES a destroyed building: a tile of rubble."""
    img, d = new(64, 64)
    rnd = random.Random(7)
    d.ellipse([3, 14, 60, 60], fill=(12, 12, 14, 120))
    for _ in range(16):
        x, y = rnd.randint(8, 55), rnd.randint(22, 54)
        w, h = rnd.randint(5, 13), rnd.randint(4, 8)
        ramp = STEEL if rnd.random() < 0.7 else STONE
        shaded_block(d, x, y, min(60, x + w), min(60, y + h), ramp, top=3, body=2, side=1, outline=False)
    for _ in range(9):
        x, y = rnd.randint(6, 56), rnd.randint(18, 52)
        d.line([x, y, x + rnd.randint(-6, 6), y - rnd.randint(2, 7)], fill=STEEL[1])
    for _ in range(3):
        x, y = rnd.randint(14, 48), rnd.randint(26, 50)
        d.point((x, y), fill=HOT[3])
    return img
