"""
Antarctica terrain generator.

Each terrain type is ONE big seamless texture (BLOCK x BLOCK tiles of 128 px), cut into
128x128 tiles named tile_<type>_<NN>.png (NN = 01..BLOCK*BLOCK, row by row from the top-left).
The map places tile number  (y mod BLOCK) * BLOCK + (x mod BLOCK) + 1  at world tile (x, y),
so neighbouring tiles always continue each other and the pattern only repeats every BLOCK tiles.

Everything is periodic (noise lattices wrap, Voronoi distances wrap), so the big texture
also tiles with itself.  Light comes from the top-left (ART_GUIDE rule).

Usage:  python3 generate_antarctica.py <output_dir> [--preview <dir>]
"""
import sys, os
import numpy as np
from PIL import Image

TILE = 64
BLOCK = 8
S = TILE * BLOCK          # 512 px big texture
LIGHT = np.array([-1.0, -1.0]) / np.sqrt(2)   # direction towards the light (x right, y down)

yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)

# ------------------------------------------------------------------ periodic primitives
def _smooth(t):
    return t * t * t * (t * (t * 6 - 15) + 10)

def value_noise(rng, gx, gy):
    """Periodic noise with gx x gy lattice cells (anisotropic allowed). Uses gradient noise
    so there are no square lattice artefacts lining up with the 128 px tile grid."""
    return perlin(rng, gx, gy=gy)

def perlin(rng, g, X=None, Y=None, gy=None):
    """Periodic gradient (Perlin) noise: g cells across horizontally, gy (default g) vertically.
    Returns ~[0,1]."""
    gyc = g if gy is None else gy
    X = xx if X is None else X
    Y = yy if Y is None else Y
    ang = rng.random((gyc, g)).astype(np.float32) * 2 * np.pi
    gxs = np.cos(ang); gys = np.sin(ang)
    u = (X % S) * g / S; v = (Y % S) * gyc / S
    i0 = np.floor(u).astype(np.int32); j0 = np.floor(v).astype(np.int32)
    fu = u - i0; fv = v - j0
    i0 %= g; j0 %= gyc; i1 = (i0 + 1) % g; j1 = (j0 + 1) % gyc
    def dot(i, j, ox, oy):
        return gxs[j, i] * ox + gys[j, i] * oy
    n00 = dot(i0, j0, fu, fv); n10 = dot(i1, j0, fu - 1, fv)
    n01 = dot(i0, j1, fu, fv - 1); n11 = dot(i1, j1, fu - 1, fv - 1)
    su = _smooth(fu); sv = _smooth(fv)
    n = (n00 * (1 - su) + n10 * su) * (1 - sv) + (n01 * (1 - su) + n11 * su) * sv
    return np.clip(n * 0.75 + 0.5, 0, 1)

def fbm(rng, octaves):
    """octaves: list of (gx, gy, weight)."""
    out = np.zeros((S, S), np.float32); tw = 0
    for gx, gy, w in octaves:
        out += w * value_noise(rng, gx, gy); tw += w
    return out / tw

def voronoi(rng, count, X=None, Y=None):
    """Periodic Voronoi on (optionally warped) coordinates. Returns F1, F2 (px) and cell id."""
    X = xx if X is None else X % S
    Y = yy if Y is None else Y % S
    pts = rng.random((count, 2)) * S
    f1 = np.full((S, S), 1e9, np.float32); f2 = f1.copy(); cid = np.zeros((S, S), np.int32)
    for k, (px, py) in enumerate(pts):
        dx = np.abs(X - px); dx = np.minimum(dx, S - dx)
        dy = np.abs(Y - py); dy = np.minimum(dy, S - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < f1
        f2 = np.where(closer, f1, np.minimum(f2, d))
        cid = np.where(closer, k, cid)
        f1 = np.where(closer, d, f1)
    return f1, f2, cid

def shade(h, strength):
    """+ = facing the light (top-left), - = facing away. Periodic gradient."""
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    # a slope that rises towards the bottom-right faces the top-left light
    return np.clip((gx + gy) / np.sqrt(2) * -strength * -1, -1, 1) * -1

BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16.0 - 0.47
BAYER = np.tile(BAYER4, (S // 4, S // 4))

def quantize(v, n, dither):
    return np.clip(np.floor(np.clip(v, 0, 0.9999) * n + BAYER * dither), 0, n - 1).astype(np.int32)

def clean_orphans(idx, passes=1):
    for _ in range(passes):
        n1 = np.roll(idx, 1, 0); n2 = np.roll(idx, -1, 0); n3 = np.roll(idx, 1, 1); n4 = np.roll(idx, -1, 1)
        same = (n1 == n2) & (n2 == n3) & (n3 == n4) & (n1 != idx)
        idx = np.where(same, n1, idx)
    return idx

def pal(idx, palette):
    return np.array(palette, np.float32)[idx]

def warp_coords(rng, amount, octaves):
    """Periodic displacement field -> wiggly but still seamless coordinates."""
    wx = (fbm(rng, octaves) - 0.5) * 2 * amount
    wy = (fbm(rng, octaves) - 0.5) * 2 * amount
    return xx + wx, yy + wy

def ridged(rng, octaves, X=None, Y=None):
    """Ridged Perlin: sharp crests. octaves: list of (g, weight)."""
    out = np.zeros((S, S), np.float32); tw = 0
    for g, w in octaves:
        n = perlin(rng, g, X, Y)
        out += w * (1 - np.abs(n * 2 - 1)) ** 1.5; tw += w
    return out / tw

def cell_edges(cid):
    """1-px boundary between Voronoi cells (never fattens, even with heavy warping). Periodic."""
    return (cid != np.roll(cid, -1, 1)) | (cid != np.roll(cid, -1, 0))

def shift(mask, dy, dx):
    return np.roll(np.roll(mask, dy, 0), dx, 1)

# ------------------------------------------------------------------ palettes (dark -> light), cool
SNOW = [(160, 178, 207), (186, 202, 226), (207, 220, 238), (225, 234, 247), (241, 246, 253)]
SNOW_SPARKLE = (255, 255, 255)
ICE = [(58, 104, 160), (78, 130, 184), (101, 155, 204), (130, 180, 221), (165, 205, 235)]
ICE_DEEP_CRACK = (36, 70, 118)
ICE_LIT_EDGE = (198, 226, 246)
ICE_BUBBLE = (222, 239, 252)
ROCK = [(34, 33, 41), (50, 49, 58), (68, 66, 76), (89, 86, 96), (114, 110, 119)]

# ------------------------------------------------------------------ SNOW: wind-packed plateau
def make_snow(seed):
    rng = np.random.default_rng(seed)
    # Wind blows roughly left->right (katabatic), so features are stretched horizontally.
    swell = fbm(rng, [(4, 4, 1.0), (8, 8, 0.5)])                       # big gentle undulation
    drifts = fbm(rng, [(6, 24, 1.0), (12, 48, 0.45)])                  # long drifts along the wind
    sastr_field = fbm(rng, [(24, 96, 1.0), (48, 160, 0.4)])            # small sharp ridges
    sastr_where = fbm(rng, [(4, 4, 1.0), (8, 8, 0.6)])                 # ridges only in some areas
    grain = fbm(rng, [(64, 64, 1.0)])

    # sastrugi crests: thin bands where the elongated noise crosses a level
    crest = np.clip(1 - np.abs(sastr_field - 0.55) * 9, 0, 1) ** 2
    crest *= np.clip((sastr_where - 0.56) * 5, 0, 1)

    height = swell * 6 + drifts * 4.5 + crest * 0.6
    light = shade(height, 11.00)
    v = 0.62 + light * 0.5 + (swell - 0.5) * 0.22 + (grain - 0.5) * 0.04
    idx = clean_orphans(quantize(v, len(SNOW), 0.15), 3)
    rgb = pal(idx, SNOW)

    spark_rng = rng.random((S, S))
    sparkle = (spark_rng > 0.999) & (idx >= 4)
    rgb[sparkle] = SNOW_SPARKLE
    return rgb

# ------------------------------------------------------------------ BLUE ICE: wind-scoured, cracked
def make_ice(seed):
    rng = np.random.default_rng(seed)
    broad = fbm(rng, [(4, 4, 1.0), (8, 8, 0.6), (16, 16, 0.25)])
    streak = fbm(rng, [(4, 32, 1.0), (8, 64, 0.5)])                    # wind-polish streaks
    fine = fbm(rng, [(64, 64, 1.0)])
    # shallow ablation scallops: soft dimples that catch the light
    sc1, _, _ = voronoi(rng, 60)
    scallop = np.minimum(sc1, 18) / 18.0

    height = broad * 5 + scallop * 1.1
    v = 0.26 + broad * 0.38 + (streak - 0.5) * 0.25 + shade(height, 7.00) * 0.24 + (fine - 0.5) * 0.05
    idx = clean_orphans(quantize(v, len(ICE), 0.2), 3)
    rgb = pal(idx, ICE)

    # Cracks: a few large wandering fractures + some finer ones. Warped => curvy, still seamless.
    WX, WY = warp_coords(rng, 17, [(8, 8, 1.0), (16, 16, 0.5)])
    f1, f2, fid = voronoi(rng, 9, WX, WY)
    fsel = rng.random(9)[fid]
    VX, VY = warp_coords(rng, 7, [(16, 16, 1.0), (32, 32, 0.5)])
    g1, g2, gid = voronoi(rng, 40, VX, VY)
    gsel = rng.random(40)[gid]
    big = cell_edges(fid)
    small = cell_edges(gid) & (gsel < 0.22) & (fsel < 0.7)
    crack = big | small
    lip = shift(crack, 1, 1) & ~crack              # light catches the bottom-right wall
    dark = shift(crack, -1, -1) & ~crack & ~lip    # top-left wall in shadow
    rgb[dark] = rgb[dark] * 0.8
    rgb[lip] = rgb[lip] * 0.35 + np.array(ICE_LIT_EDGE, np.float32) * 0.65
    rgb[crack] = ICE_DEEP_CRACK

    # Wind-blown snow patches: stretched along the wind, smooth soft shading.
    patch = fbm(rng, [(6, 18, 1.0), (12, 36, 0.5), (24, 72, 0.15)])
    snowy = patch > 0.64
    body = np.clip((patch - 0.64) * 6, 0, 1)        # thicker in the middle
    sv = 0.55 + body * 0.25 + shade(patch * 9, 10.00) * 0.35
    srgb = pal(clean_orphans(quantize(sv, len(SNOW), 0.2), 2), SNOW)
    rgb[snowy] = srgb[snowy]
    rim = snowy & ~shift(snowy, -1, -1)
    rgb[rim] = np.array(SNOW[4], np.float32)
    drop = snowy & ~shift(snowy, 1, 1)               # thin shadow on the ice at the lee side
    rgb[shift(drop, 1, 1) & ~snowy] *= 0.85

    bub = (rng.random((S, S)) > 0.999) & ~crack & ~snowy & ~lip
    rgb[bub] = ICE_BUBBLE
    return rgb

# ------------------------------------------------------------------ ROCK: exposed nunatak with snow in hollows
def make_rock(seed):
    rng = np.random.default_rng(seed)
    broad = fbm(rng, [(4, 4, 1.0), (8, 8, 0.6)])
    CX, CY = warp_coords(rng, 20, [(4, 4, 1.0), (8, 8, 0.5)])
    crags = ridged(rng, [(6, 1.0), (12, 0.5), (24, 0.2)], CX, CY)  # sharp crests
    fine = fbm(rng, [(32, 32, 1.0)])

    height = broad * 8 + crags * 5 + fine * 0.8
    light = shade(height, 3.75)
    v = 0.42 + light * 0.55 + (crags - 0.5) * 0.25 + (fine - 0.5) * 0.12
    idx = clean_orphans(quantize(v, len(ROCK), 0.2), 3)
    rgb = pal(idx, ROCK)

    # A few fractures through the rock.
    WX, WY = warp_coords(rng, 13, [(8, 8, 1.0), (16, 16, 0.5)])
    f1, f2, fid = voronoi(rng, 14, WX, WY)
    fsel = rng.random(14)[fid]
    frac = cell_edges(fid) & (fsel < 0.55)
    rgb[frac] = np.array(ROCK[0], np.float32)
    lit_edge = shift(frac, 1, 1) & ~frac
    rgb[lit_edge] = np.array(ROCK[3], np.float32)

    # Snow fills the low ground between crags; smooth, wind-shaped.
    snowf = fbm(rng, [(6, 12, 1.0), (12, 24, 0.5), (32, 64, 0.15)])
    low = (1 - broad) * 0.5 + (1 - crags) * 0.35 + snowf * 0.45
    snowy = low > 0.705
    body = np.clip((low - 0.705) * 8, 0, 1)
    sv = 0.55 + body * 0.25 + shade(snowf * 8 + broad * 3, 9.00) * 0.35
    srgb = pal(clean_orphans(quantize(sv, len(SNOW), 0.2), 2), SNOW)
    rgb[snowy] = srgb[snowy]
    cast = snowy & shift(~snowy, 1, 1)               # rock casts a short blue shadow onto snow
    rgb[cast] = np.array(SNOW[1], np.float32)
    cast2 = snowy & shift(~snowy, 2, 2) & ~cast
    rgb[cast2] = np.array(SNOW[2], np.float32)
    rim = snowy & ~shift(snowy, -1, -1) & ~cast
    rgb[rim] = np.array(SNOW[4], np.float32)

    speck = (rng.random((S, S)) > 0.999) & ~snowy
    rgb[speck] = np.array(ROCK[4], np.float32)
    return rgb

TYPES = {"snow": (make_snow, 4101), "ice": (make_ice, 5202), "rock": (make_rock, 6303)}

# ------------------------------------------------------------------ output
def to_img(rgb):
    return Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "RGB").convert("RGBA")

def generate(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    bigs = {}
    for name, (fn, seed) in TYPES.items():
        big = to_img(fn(seed))
        bigs[name] = big
        for ty in range(BLOCK):
            for tx in range(BLOCK):
                n = ty * BLOCK + tx + 1
                big.crop((tx * TILE, ty * TILE, (tx + 1) * TILE, (ty + 1) * TILE)).save(
                    os.path.join(out_dir, f"tile_{name}_{n:02d}.png"))
    return bigs

def check_seamless(bigs):
    """The big texture must wrap: compare opposite edges' continuity via gradient size."""
    for name, img in bigs.items():
        a = np.asarray(img).astype(int)[:, :, :3]
        inner = np.abs(np.diff(a, axis=1)).mean()
        wrap = np.abs(a[:, 0] - a[:, -1]).mean()
        inner_v = np.abs(np.diff(a, axis=0)).mean()
        wrap_v = np.abs(a[0] - a[-1]).mean()
        print(f"{name}: avg neighbour diff inside {inner:.1f}/{inner_v:.1f}, across wrap {wrap:.1f}/{wrap_v:.1f}")

def preview(bigs, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name, img in bigs.items():
        # 1.5 x 1.5 repeats so the wrap seam is visible if it exists
        sheet = Image.new("RGBA", (int(S * 1.5), int(S * 1.5)))
        for oy in (0, S):
            for ox in (0, S):
                sheet.paste(img, (ox, oy))
        sheet.save(os.path.join(out_dir, f"sheet_{name}.png"))
        img.crop((0, 0, 384, 384)).resize((768, 768), Image.NEAREST).save(os.path.join(out_dir, f"zoom_{name}.png"))
    # what the player sees: a 1920x1080 view (15 x 8.4 tiles) of each type side by side
    view = Image.new("RGBA", (1920, 1080))
    for i, name in enumerate(bigs):
        view.paste(bigs[name].crop((0, 0, 640, 1080 if S >= 1080 else S)), (i * 640, 0))
    view.save(os.path.join(out_dir, "view_all.png"))

if __name__ == "__main__":
    bigs = generate(sys.argv[1])
    check_seamless(bigs)
    if "--preview" in sys.argv:
        preview(bigs, sys.argv[sys.argv.index("--preview") + 1])
