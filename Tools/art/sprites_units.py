"""Player, conveyor belts and data wires. Same house style: 3/4 view, light from the top-left."""
import math
from palette import *

# ---------------------------------------------------------------- player (64x64)
# A figure in a hooded cold suit, seen from above and slightly behind.
# Feet sit near the bottom of the tile so the sprite lines up with the tile it stands on.
VISOR = [rgb(c) for c in ('#07171E', '#0D2F3C', '#15566B', '#2E97B0', '#6FD8E8', '#BFF4FA')]
PACK  = [rgb(c) for c in ('#140F18', '#241A2A', '#3A2B42', '#55405F', '#75597E', '#9878A0')]

def _legs(d, cx, base, phase, facing):
    """Two legs; phase -1..1 swings them. base = y of the ground contact."""
    swing = int(round(phase * 6))
    for side, sw in ((-1, swing), (1, -swing)):
        x = cx + side * 4
        knee_y = base - 8
        foot_x = x + (sw if facing else sw // 2)
        foot_y = base - abs(sw) // 3
        d.line([x, base - 12, x, knee_y], fill=SUIT[2], width=4)
        d.line([x, knee_y, foot_x, foot_y], fill=SUIT[1], width=4)
        d.line([foot_x - 1, foot_y, foot_x + 2, foot_y], fill=SUIT[0])
        d.point((x - 1, base - 11), fill=SUIT[4])

def _body(d, cx, base, lean=0):
    shaded_block(d, cx - 8 + lean, base - 26, cx + 8 + lean, base - 10, SUIT, top=4, body=2, side=1)
    # chest light
    led(d, cx - 1 + lean, base - 20, VISOR, 2)

def _pack(d, cx, base, lean=0):
    shaded_block(d, cx - 6 + lean, base - 30, cx + 6 + lean, base - 22, PACK, top=4, body=2, side=1)

def _hood(d, cx, top, face):
    """face: 'down' shows a visor, 'up' shows the back of the hood, 'side' a profile."""
    d.ellipse([cx - 9, top, cx + 9, top + 17], fill=SUIT[2], outline=OUTLINE)
    d.arc([cx - 9, top, cx + 9, top + 17], 160, 340, fill=SUIT[4])
    if face == 'down':
        d.ellipse([cx - 6, top + 5, cx + 6, top + 14], fill=VISOR[1], outline=OUTLINE)
        d.ellipse([cx - 4, top + 6, cx + 2, top + 10], fill=VISOR[3])
        d.point((cx - 3, top + 7), fill=VISOR[5])
    elif face == 'side':
        d.ellipse([cx - 1, top + 5, cx + 8, top + 13], fill=VISOR[1], outline=OUTLINE)
        d.ellipse([cx + 1, top + 6, cx + 5, top + 9], fill=VISOR[3])
    else:
        d.ellipse([cx - 6, top + 4, cx + 6, top + 13], fill=SUIT[1])
        hline(d, cx - 5, cx + 5, top + 6, SUIT[3])

def player(direction, phase=0.0):
    img, d = new(TILE, TILE)
    cx, base = 32, 54 - (1 if abs(phase) > 0.7 else 0)      # slight bob at the top of each step
    if direction == 'up':
        _pack(d, cx, base)
        _legs(d, cx, base, phase, False)
        _body(d, cx, base)
        _pack(d, cx, base)
        _hood(d, cx, base - 44, 'up')
    elif direction == 'side':
        _legs(d, cx, base, phase, True)
        _pack(d, cx - 4, base)
        _body(d, cx + 1, base)
        # forward arm swings opposite the legs
        ax = cx + 7
        d.line([ax, base - 23, ax + 4, base - 16 - int(phase * 3)], fill=SUIT[3], width=3)
        d.point((ax + 4, base - 16 - int(phase * 3)), fill=SUIT[5])
        _hood(d, cx + 2, base - 44, 'side')
    else:
        _legs(d, cx, base, phase, False)
        _body(d, cx, base)
        for side in (-1, 1):                       # arms
            ax = cx + side * 10
            d.line([ax, base - 24, ax, base - 14 + int(side * phase * 2)], fill=SUIT[3], width=3)
            d.point((ax, base - 23), fill=SUIT[5])
        _hood(d, cx, base - 44, 'down')
    return img

def player_walk_sheet(direction, frames=4):
    """One row of frames, left to right."""
    img, _ = new(TILE * frames, TILE)
    for i in range(frames):
        phase = math.sin(i / frames * 2 * math.pi)
        img.alpha_composite(player(direction, phase), (i * TILE, 0))
    return img

# ---------------------------------------------------------------- belts (64x64 per frame)
def _belt_tile(direction, frame, frames, ramp, accent, thin=False):
    """A sunken belt: dark treaded surface with two muted chevrons, tier colour on the rails only."""
    img, d = new(TILE, TILE)
    horiz = direction in ('right', 'left')
    rail = 10 if not thin else 14
    lo, hi = rail, TILE - 1 - rail

    if horiz:
        box(d, 0, lo, TILE - 1, hi, COAL[2])
    else:
        box(d, lo, 0, hi, TILE - 1, COAL[2])

    # rails, always lit from the top-left, with a thin tier-coloured stripe
    if horiz:
        shaded_block(d, 0, 0, TILE - 1, lo - 1, ramp, top=5, body=3, side=1, outline=False)
        shaded_block(d, 0, hi + 1, TILE - 1, TILE - 1, ramp, top=4, body=2, side=1, outline=False)
        hline(d, 0, TILE - 1, lo - 2, accent[2])
        hline(d, 0, TILE - 1, hi + 2, accent[1])
    else:
        shaded_block(d, 0, 0, lo - 1, TILE - 1, ramp, top=5, body=3, side=1, outline=False)
        shaded_block(d, hi + 1, 0, TILE - 1, TILE - 1, ramp, top=3, body=2, side=1, outline=False)
        vline(d, lo - 2, 0, TILE - 1, accent[2])
        vline(d, hi + 2, 0, TILE - 1, accent[1])

    # two chevrons per tile, crawling the way the belt runs
    step = TILE // 2
    off = int(frame / frames * step)
    sign = 1 if direction in ('right', 'up') else -1
    depth = (hi - lo) // 2
    for k in range(-1, 3):
        p = (k * step + sign * off) % (TILE + step) - step // 2
        if horiz:
            x = p if direction == 'right' else TILE - 1 - p
            dx = 1 if direction == 'right' else -1
            for i in range(depth):
                for xx in (x + dx * i,):
                    if 0 <= xx < TILE:
                        d.point((xx, lo + 2 + i), fill=COAL[4])
                        d.point((xx, hi - 2 - i), fill=COAL[4])
            if 0 <= x + dx * depth < TILE:
                d.point((x + dx * depth, lo + 2 + depth), fill=COAL[5])
        else:
            y = TILE - 1 - p if direction == 'up' else p
            dy = -1 if direction == 'up' else 1
            for i in range(depth):
                yy = y + dy * i
                if 0 <= yy < TILE:
                    d.point((lo + 2 + i, yy), fill=COAL[4])
                    d.point((hi - 2 - i, yy), fill=COAL[4])
            if 0 <= y + dy * depth < TILE:
                d.point((lo + 2 + depth, y + dy * depth), fill=COAL[5])
    return img

def belt_sheet(direction, frames=4):
    img, _ = new(TILE * frames, TILE)
    for i in range(frames):
        img.alpha_composite(_belt_tile(direction, i, frames, IRON, HOT), (i * TILE, 0))
    return img

def wire_sheet(direction, frames=4):
    img, _ = new(TILE * frames, TILE)
    for i in range(frames):
        img.alpha_composite(_belt_tile(direction, i, frames, STEEL, DATA, thin=True), (i * TILE, 0))
    return img
