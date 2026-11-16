"""Machine sprites: 3/4 top-down, light from the top-left, dark industrial palette."""
import math
from palette import *

def vents(d, x0, y0, x1, y1, ramp, step=5):
    """A louvred vent panel: dark slots with a lit lip, breaks up big flat roofs."""
    shaded_block(d, x0, y0, x1, y1, ramp, top=4, body=2, side=1)
    for y in range(y0 + 3, y1 - 1, step):
        hline(d, x0 + 3, x1 - 3, y, COAL[1])
        hline(d, x0 + 3, x1 - 3, y + 1, ramp[4])

def pipe(d, x0, y0, x1, y1, ramp, w=5):
    """A straight horizontal or vertical pipe run, highlit along its top-left edge."""
    if x0 > x1: x0, x1 = x1, x0
    if y0 > y1: y0, y1 = y1, y0
    if y1 - y0 <= w:
        box(d, x0, y0, x1, y0 + w, ramp[2]); hline(d, x0, x1, y0, ramp[5]); hline(d, x0, x1, y0 + w, ramp[1])
    else:
        box(d, x0, y0, x0 + w, y1, ramp[2]); vline(d, x0, y0, y1, ramp[5]); vline(d, x0 + w, y0, y1, ramp[1])

def strut(d, x0, y0, x1, y1, ramp, w=5):
    """A thick strut between any two points (used for angled hydraulics)."""
    d.line([x0, y0, x1, y1], fill=ramp[1], width=w + 2)
    d.line([x0, y0, x1, y1], fill=ramp[3], width=w)
    d.line([x0, y0, x1, y1], fill=ramp[5])

# ---------------------------------------------------------------- crashed time pod (3x3)
def crashpod(reactor=1.0):
    img, d = new(3 * TILE, 3 * TILE + OVERHANG)
    W, H = img.size
    glow = HOT[4] if reactor > 0.6 else HOT[3] if reactor > 0.25 else HOT[2]
    # gouge in the ice behind the pod, where it skidded in
    d.polygon([(6, H - 30), (W - 40, H - 54), (W - 26, H - 20), (18, H - 8)], fill=(26, 34, 50, 120))

    # hull: a blunt capsule lying nose-down-left, drawn as a tapered body
    hull = [(26, 52), (74, 20), (150, 26), (176, 62), (172, 150), (120, 182), (48, 174), (22, 132)]
    d.polygon([(x + 2, y + 3) for x, y in hull], fill=OUTLINE)
    d.polygon(hull, fill=STEEL[3])
    d.polygon([(34, 56), (76, 30), (146, 36), (166, 66), (120, 92), (52, 86)], fill=STEEL[4])
    d.line([(26, 52), (74, 20)], fill=STEEL[5]); d.line([(74, 20), (150, 26)], fill=STEEL[5])
    d.line([(172, 150), (120, 182)], fill=STEEL[1]); d.line([(120, 182), (48, 174)], fill=STEEL[1])

    # buckled plating
    for i, y in enumerate((70, 96, 122, 148)):
        d.line([30 + i * 3, y, 168 - i * 4, y + 4], fill=STEEL[1])
        d.line([30 + i * 3, y + 1, 168 - i * 4, y + 5], fill=STEEL[5])

    # torn-open bay, centre: the dying reactor
    bay = [(62, 74), (132, 68), (142, 140), (58, 146)]
    d.polygon(bay, fill=COAL[0], outline=OUTLINE)
    d.polygon([(70, 82), (126, 77), (134, 133), (66, 138)], fill=glow)
    d.ellipse([84, 88, 118, 126], fill=HOT[5] if reactor > 0.5 else HOT[4], outline=OUTLINE)
    d.ellipse([92, 96, 110, 118], fill=SNOW[5] if reactor > 0.6 else HOT[5])
    # torn metal teeth around the bay
    for k in range(6):
        x = 64 + k * 13
        d.polygon([(x, 72), (x + 6, 64), (x + 10, 73)], fill=STEEL[2], outline=OUTLINE)
        d.polygon([(x, 143), (x + 6, 151), (x + 10, 142)], fill=STEEL[2], outline=OUTLINE)

    # cracks leaking reactor light
    for (ax, ay, bx, by) in ((142, 60, 168, 44), (46, 96, 24, 88), (140, 150, 166, 164), (52, 150, 36, 166)):
        d.line([ax, ay, bx, by], fill=OUTLINE)
        d.line([ax, ay + 1, bx, by + 1], fill=glow)

    # snapped antenna
    d.line([150, 36, 166, 8], fill=STEEL[4]); d.line([166, 8, 158, 16], fill=STEEL[2])
    d.point((166, 8), fill=glow)
    # scattered debris
    for (dx, dy, dw) in ((10, 150, 9), (182, 120, 7), (6, 96, 6), (186, 176, 8)):
        shaded_block(d, dx, dy, dx + dw, dy + dw - 2, STEEL, top=4, body=2, side=1)
    # snow banked up against the downhill side
    for x in range(2, W - 2):
        h = int(10 + 7 * math.sin(x * 0.11) + 4 * math.sin(x * 0.31))
        vline(d, x, H - h, H - 2, SNOW[3])
        d.point((x, H - h), fill=SNOW[5])
    return img

# ---------------------------------------------------------------- drill / Miner MK1 (3x3)
def drill(spin=0.0, output=None, hot=False):
    img, d, roof, wall = building_shell(3, 3, STEEL)
    rx0, ry0, rx1, ry1 = roof
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2
    vents(d, rx0 + 22, ry0 + 4, rx1 - 22, ry0 + 20, STEEL)
    vents(d, rx0 + 22, ry1 - 20, rx1 - 22, ry1 - 4, STEEL)
    # corner anchor feet
    for ax, ay in ((rx0 + 4, ry0 + 4), (rx1 - 18, ry0 + 4), (rx0 + 4, ry1 - 16), (rx1 - 18, ry1 - 16)):
        shaded_block(d, ax, ay, ax + 13, ay + 11, IRON, top=4, body=2, side=1)
    # hydraulic rams reaching in to the head
    for k in range(4):
        a = k * 1.5708 + 0.785
        strut(d, cx + int(30 * math.cos(a)), cy + int(30 * math.sin(a)),
              cx + int(52 * math.cos(a)), cy + int(52 * math.sin(a)), IRON, 5)
    # drill housing
    d.ellipse([cx - 34, cy - 34, cx + 34, cy + 34], fill=STEEL[2], outline=OUTLINE)
    d.arc([cx - 34, cy - 34, cx + 34, cy + 34], 170, 330, fill=STEEL[5])
    d.ellipse([cx - 27, cy - 27, cx + 27, cy + 27], fill=COAL[1], outline=OUTLINE)
    # three rotating cutter arms
    for k in range(3):
        a = spin + k * 2.094
        x2, y2 = cx + int(23 * math.cos(a)), cy + int(23 * math.sin(a))
        d.line([cx, cy, x2, y2], fill=IRON[4], width=5)
        d.line([cx, cy, x2, y2], fill=IRON[5])
        d.ellipse([x2 - 5, y2 - 5, x2 + 5, y2 + 5], fill=IRON[3], outline=OUTLINE)
        d.point((x2 - 1, y2 - 1), fill=IRON[5])
    d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=HOT[3] if hot else IRON[2], outline=OUTLINE)
    if hot:
        d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=HOT[5])
    # output chutes on all four sides; the live one lights up
    chutes = {'up': (cx - 9, ry0 + 1, cx + 9, ry0 + 12), 'down': (cx - 9, ry1 - 12, cx + 9, ry1 - 1),
              'left': (rx0 + 1, cy - 9, rx0 + 12, cy + 9), 'right': (rx1 - 12, cy - 9, rx1 - 1, cy + 9)}
    for name, (a, b, c2, e) in chutes.items():
        live = (name == output)
        shaded_block(d, a, b, c2, e, IRON, top=4, body=3 if live else 2, side=1)
        if live:
            box(d, a + 2, b + 2, c2 - 2, e - 2, HOT[3])
    # status lights on the front face
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 6, wy0 + 6, wx1 - 6, wy1 - 6, STEEL[1])
    for i in range(6):
        led(d, wx0 + 12 + i * 26, wy0 + 9, GREENLED if (hot and i <= (spin * 2) % 6) else COAL, 3)
    hline(d, wx0 + 6, wx1 - 6, wy0 + 6, STEEL[4])
    return img

# ---------------------------------------------------------------- smelter (2x2)
def smelter(heat=0.0):
    img, d, roof, wall = building_shell(2, 2, STEEL)
    rx0, ry0, rx1, ry1 = roof
    panel_lines(d, roof, STEEL, step=14)
    # chimney, back-left
    shaded_block(d, rx0 + 8, ry0 + 2, rx0 + 30, ry0 + 26, IRON, top=4, body=2, side=1)
    box(d, rx0 + 12, ry0 + 5, rx0 + 26, ry0 + 10, COAL[1])
    if heat > 0.15:                                  # embers in the flue
        box(d, rx0 + 14, ry0 + 6, rx0 + 24, ry0 + 8, HOT[2 + int(heat * 2)])
    # flue running from the crucible to the chimney
    pipe(d, rx0 + 30, ry0 + 10, rx0 + 54, ry0 + 10, IRON, 7)
    # crucible: a thick ring with molten metal inside
    cx, cy = (rx0 + rx1) // 2 + 10, (ry0 + ry1) // 2 + 6
    d.ellipse([cx - 26, cy - 21, cx + 26, cy + 21], fill=IRON[1], outline=OUTLINE)
    d.ellipse([cx - 22, cy - 18, cx + 22, cy + 18], fill=IRON[3])
    d.arc([cx - 26, cy - 21, cx + 26, cy + 21], 165, 335, fill=IRON[5])
    ring = HOT[1 + min(4, int(heat * 5))] if heat > 0 else COAL[1]
    d.ellipse([cx - 15, cy - 12, cx + 15, cy + 12], fill=ring, outline=OUTLINE)
    if heat > 0.4:
        d.ellipse([cx - 8, cy - 6, cx + 8, cy + 6], fill=HOT[5])
        d.point((cx - 3, cy - 2), fill=SNOW[5])
    for k in range(8):                                   # bolts around the rim
        a = k * 0.785
        d.point((cx + int(24 * math.cos(a)), cy + int(19 * math.sin(a))), fill=IRON[5])
    # front door: glows with the heat
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 18, wy0 + 4, wx1 - 18, wy1 - 4, COAL[1])
    if heat > 0:
        g = HOT[1 + min(4, int(heat * 5))]
        box(d, wx0 + 21, wy0 + 6, wx1 - 21, wy1 - 6, g)
        for x in range(wx0 + 22, wx1 - 21, 4):       # grate bars
            vline(d, x, wy0 + 6, wy1 - 6, COAL[2])
    hline(d, wx0 + 18, wx1 - 18, wy0 + 4, STEEL[4])
    led(d, wx0 + 6, wy0 + 8, GREENLED if heat > 0 else COAL, 3)
    led(d, wx1 - 9, wy0 + 8, HOT if heat > 0.7 else COAL, 3)
    return img

# ---------------------------------------------------------------- assembler (3x3)
def assembler(spin=0.0, working=False):
    img, d, roof, wall = building_shell(3, 3, STEEL)
    rx0, ry0, rx1, ry1 = roof
    vents(d, rx0 + 4, ry0 + 20, rx0 + 20, ry1 - 20, STEEL, 6)
    vents(d, rx1 - 20, ry0 + 20, rx1 - 4, ry1 - 20, STEEL, 6)
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2
    # glass dome over the work area
    d.ellipse([cx - 34, cy - 30, cx + 34, cy + 30], fill=STEEL[2], outline=OUTLINE)
    d.ellipse([cx - 29, cy - 25, cx + 29, cy + 25], fill=GREENLED[1] if working else STEEL[1])
    d.arc([cx - 34, cy - 30, cx + 34, cy + 30], 170, 330, fill=STEEL[5])
    # two assembly arms meeting in the middle
    for k in (0, 1):
        a = spin + k * 3.14159
        ex, ey = cx + int(20 * math.cos(a)), cy + int(15 * math.sin(a))
        mx, my = cx + int(11 * math.cos(a + 0.7)), cy + int(9 * math.sin(a + 0.7))
        d.line([cx, cy, mx, my], fill=IRON[4], width=3)
        d.line([mx, my, ex, ey], fill=IRON[3], width=3)
        d.ellipse([ex - 3, ey - 3, ex + 3, ey + 3], fill=IRON[5], outline=OUTLINE)
    if working:
        d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=GREENLED[5])
    # corner pillars
    for ax, ay in ((rx0 + 3, ry0 + 3), (rx1 - 16, ry0 + 3), (rx0 + 3, ry1 - 15), (rx1 - 16, ry1 - 15)):
        shaded_block(d, ax, ay, ax + 12, ay + 11, IRON, top=4, body=2, side=1)
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 8, wy0 + 5, wx1 - 8, wy1 - 5, STEEL[1])
    for i in range(5):
        led(d, wx0 + 16 + i * 30, wy0 + 9, GREENLED if working else COAL, 3)
    return img

# ---------------------------------------------------------------- data cell encoder (2x2)
def encoder(pulse=0.0, working=False):
    img, d, roof, wall = building_shell(2, 2, STEEL)
    rx0, ry0, rx1, ry1 = roof
    panel_lines(d, roof, STEEL, step=13)
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2 + 2
    # containment ring
    d.ellipse([cx - 24, cy - 20, cx + 24, cy + 20], fill=STEEL[2], outline=OUTLINE)
    d.arc([cx - 24, cy - 20, cx + 24, cy + 20], 165, 335, fill=STEEL[5])
    for k in range(6):
        a = k * 1.047
        d.line([cx + int(16 * math.cos(a)), cy + int(13 * math.sin(a)),
                cx + int(23 * math.cos(a)), cy + int(19 * math.sin(a))], fill=IRON[4])
    # core
    lvl = 2 + int(pulse * 3) if working else 1
    d.ellipse([cx - 14, cy - 11, cx + 14, cy + 11], fill=DATA[min(5, lvl)])
    if working:
        d.ellipse([cx - 7, cy - 5, cx + 7, cy + 5], fill=DATA[5])
    # emitter mast
    d.line([rx1 - 14, ry0 + 20, rx1 - 14, ry0 + 4], fill=IRON[4])
    d.line([rx1 - 18, ry0 + 6, rx1 - 10, ry0 + 6], fill=IRON[5])
    if working:
        d.point((rx1 - 14, ry0 + 3), fill=DATA[5])
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 10, wy0 + 5, wx1 - 10, wy1 - 5, COAL[1])
    for i in range(4):
        led(d, wx0 + 16 + i * 22, wy0 + 9, DATA if working else COAL, 3)
    return img

# ---------------------------------------------------------------- data center L1 (4x4)
def datacenter(pulse=0.0, working=False, heat=0.0):
    img, d, roof, wall = building_shell(4, 4, STEEL)
    rx0, ry0, rx1, ry1 = roof
    # server rows
    for r in range(4):
        y = ry0 + 10 + r * 34
        shaded_block(d, rx0 + 12, y, rx1 - 12, y + 24, IRON, top=4, body=2, side=1)
        for i in range(14):
            x = rx0 + 20 + i * 14
            if x > rx1 - 20:
                break
            on = working and ((i + r * 3 + int(pulse * 6)) % 4) != 0
            led(d, x, y + 6, DATA if on else COAL, 3)
            led(d, x, y + 15, GREENLED if on else COAL, 2)
    # cooling fins down both sides
    for x in (rx0 + 3, rx1 - 9):
        for y in range(ry0 + 8, ry1 - 8, 7):
            box(d, x, y, x + 6, y + 4, STEEL[3] if heat < 0.6 else HOT[2])
            hline(d, x, x + 6, y, STEEL[5])
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 10, wy0 + 5, wx1 - 10, wy1 - 5, COAL[1])
    bars = int(10 * (heat if heat > 0 else 0))
    for i in range(10):
        c = (HOT if i < bars else COAL) if heat > 0 else COAL
        box(d, wx0 + 18 + i * 20, wy0 + 8, wx0 + 30 + i * 20, wy0 + 13, c[3 if i < bars else 1])
    return img

# ---------------------------------------------------------------- chest (1x1)
def chest():
    img, d, roof, wall = building_shell(1, 1, IRON, wall=18)
    rx0, ry0, rx1, ry1 = roof
    box(d, rx0 + 5, ry0 + 5, rx1 - 5, ry1 - 3, IRON[3])
    hline(d, rx0 + 5, rx1 - 5, ry0 + 5, IRON[5])
    vline(d, rx0 + 5, ry0 + 5, ry1 - 3, IRON[5])
    hline(d, rx0 + 5, rx1 - 5, (ry0 + ry1) // 2, IRON[1])     # lid seam
    for x in (rx0 + 12, rx1 - 13):                             # strapping
        vline(d, x, ry0 + 5, ry1 - 3, IRON[2])
        d.point((x, ry0 + 7), fill=IRON[5])
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 20, wy0 + 4, wx1 - 20, wy1 - 5, COAL[1])      # latch
    led(d, wx0 + 6, wy0 + 6, GREENLED, 2)
    return img

# ---------------------------------------------------------------- arm (1x1)
def arm(charge=0.0):
    img, d, roof, wall = building_shell(1, 1, STEEL, wall=18)
    rx0, ry0, rx1, ry1 = roof
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2
    # squat base
    d.ellipse([cx - 20, cy - 16, cx + 20, cy + 16], fill=STEEL[2], outline=OUTLINE)
    d.arc([cx - 20, cy - 16, cx + 20, cy + 16], 165, 335, fill=STEEL[5])
    # emitter ring: this is a telekinesis device, not a grabber
    lvl = 2 + int(charge * 3)
    d.ellipse([cx - 13, cy - 10, cx + 13, cy + 10], fill=COAL[1], outline=OUTLINE)
    d.arc([cx - 13, cy - 10, cx + 13, cy + 10], 0, 360, fill=DATA[min(5, lvl)])
    d.ellipse([cx - 6, cy - 4, cx + 6, cy + 4], fill=DATA[min(5, lvl)])
    d.point((cx, cy), fill=DATA[5])
    for k in range(4):                                         # four emitter nubs
        a = k * 1.5708 + 0.785
        d.ellipse([cx + int(17 * math.cos(a)) - 2, cy + int(13 * math.sin(a)) - 2,
                   cx + int(17 * math.cos(a)) + 2, cy + int(13 * math.sin(a)) + 2],
                  fill=IRON[4], outline=OUTLINE)
    wx0, wy0, wx1, wy1 = wall
    led(d, wx0 + 6, wy0 + 6, DATA if charge > 0 else COAL, 2)
    led(d, wx1 - 8, wy0 + 6, GREENLED, 2)
    return img

# ---------------------------------------------------------------- coal burner (2x2)
def coal_burner(burn=0.0):
    img, d, roof, wall = building_shell(2, 2, STEEL)
    rx0, ry0, rx1, ry1 = roof
    vents(d, rx0 + 4, ry0 + 4, rx0 + 22, ry1 - 4, STEEL, 6)
    # chimney, back-right, with smoke when alight
    shaded_block(d, rx1 - 30, ry0 + 2, rx1 - 8, ry0 + 28, IRON, top=4, body=2, side=1)
    box(d, rx1 - 26, ry0 + 5, rx1 - 12, ry0 + 11, COAL[1])
    if burn > 0.1:
        box(d, rx1 - 24, ry0 + 6, rx1 - 14, ry0 + 9, HOT[1 + int(burn * 3)])
        for k in range(3):
            y = ry0 + 2 - k * 0 + k
            d.point((rx1 - 20 + k * 2, ry0 + 2), fill=COAL[int(3 + burn)])
    # firebox: a round grate that glows with the burn
    cx, cy = (rx0 + rx1) // 2 + 6, (ry0 + ry1) // 2 + 6
    d.ellipse([cx - 24, cy - 19, cx + 24, cy + 19], fill=IRON[1], outline=OUTLINE)
    d.ellipse([cx - 20, cy - 16, cx + 20, cy + 16], fill=COAL[1])
    d.arc([cx - 24, cy - 19, cx + 24, cy + 19], 165, 335, fill=IRON[5])
    if burn > 0:
        d.ellipse([cx - 15, cy - 11, cx + 15, cy + 11], fill=HOT[1 + min(4, int(burn * 5))])
        if burn > 0.5:
            d.ellipse([cx - 8, cy - 5, cx + 8, cy + 5], fill=HOT[5])
    for x in range(cx - 18, cx + 19, 6):          # grate bars
        vline(d, x, cy - 14, cy + 14, COAL[2])
    # fuel hatch on the front face
    wx0, wy0, wx1, wy1 = wall
    box(d, wx0 + 10, wy0 + 4, wx1 - 10, wy1 - 5, COAL[1])
    hline(d, wx0 + 10, wx1 - 10, wy0 + 4, STEEL[4])
    led(d, wx0 + 5, wy0 + 8, HOT if burn > 0 else COAL, 3)
    return img

# ---------------------------------------------------------------- wind turbine (2x2)
def wind_turbine(spin=0.0):
    """A vertical-axis turbine: the sort that copes with polar wind from any direction."""
    img, d, roof, wall = building_shell(2, 2, STEEL, wall=18)
    rx0, ry0, rx1, ry1 = roof
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2
    # guy wires out to the corners
    for ax, ay in ((rx0 + 6, ry0 + 6), (rx1 - 6, ry0 + 6), (rx0 + 6, ry1 - 6), (rx1 - 6, ry1 - 6)):
        d.line([cx, cy, ax, ay], fill=STEEL[1])
        shaded_block(d, ax - 4, ay - 3, ax + 4, ay + 3, IRON, top=4, body=2, side=1, outline=False)
    # three solid curved vanes turning about the centre
    for k in range(3):
        a = spin + k * 2.094
        inner, outer = [], []
        for t in range(13):
            f = t / 12.0
            r = 9 + f * 27
            ang = a + f * 1.05
            px = cx + r * math.cos(ang)
            py = cy + r * math.sin(ang) * 0.82
            # the vane has width, tapering towards the tip
            w = 5.0 - 2.5 * f
            nx, ny = -math.sin(ang), math.cos(ang) * 0.82
            inner.append((px - nx * w, py - ny * w))
            outer.append((px + nx * w, py + ny * w))
        poly = inner + outer[::-1]
        d.polygon([(int(x + 1), int(y + 1)) for x, y in poly], fill=OUTLINE)
        d.polygon([(int(x), int(y)) for x, y in poly], fill=IRON[3])
        # lit edge along the leading side
        for i in range(len(inner) - 1):
            d.line([int(inner[i][0]), int(inner[i][1]), int(inner[i + 1][0]), int(inner[i + 1][1])], fill=IRON[5])
    d.ellipse([cx - 9, cy - 8, cx + 9, cy + 8], fill=STEEL[2], outline=OUTLINE)
    d.ellipse([cx - 4, cy - 3, cx + 4, cy + 3], fill=STEEL[5])
    wx0, wy0, wx1, wy1 = wall
    led(d, wx0 + 6, wy0 + 6, GREENLED, 2)
    return img

# ---------------------------------------------------------------- battery bank (1x1)
def battery(charge=0.0):
    img, d, roof, wall = building_shell(1, 1, STEEL, wall=18)
    rx0, ry0, rx1, ry1 = roof
    # four cells side by side
    for i in range(4):
        x = rx0 + 5 + i * 13
        shaded_block(d, x, ry0 + 6, x + 10, ry1 - 5, IRON, top=4, body=2, side=1, outline=False)
        lit = charge * 4 > i
        box(d, x + 2, ry0 + 9, x + 8, ry1 - 8, (GREENLED if lit else COAL)[3 if lit else 1])
        d.point((x + 5, ry0 + 5), fill=COPPER[4])       # terminal
    hline(d, rx0 + 5, rx1 - 5, ry0 + 4, COPPER[3])      # bus bar
    wx0, wy0, wx1, wy1 = wall
    led(d, wx0 + 6, wy0 + 6, GREENLED if charge > 0.05 else WARN, 2)
    return img

# ---------------------------------------------------------------- router (1x1)
def router():
    """A flat junction plate: items can come in from any side and leave by any other."""
    img, d, roof, wall = building_shell(1, 1, STEEL, wall=14)
    rx0, ry0, rx1, ry1 = roof
    cx, cy = (rx0 + rx1) // 2, (ry0 + ry1) // 2
    box(d, rx0 + 4, ry0 + 4, rx1 - 4, ry1 - 4, COAL[2])
    # four mouths
    for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        mx, my = cx + dx * 20, cy + dy * 16
        shaded_block(d, mx - 7, my - 5, mx + 7, my + 5, IRON, top=4, body=2, side=1, outline=False)
    # turntable
    d.ellipse([cx - 13, cy - 11, cx + 13, cy + 11], fill=IRON[3], outline=OUTLINE)
    d.arc([cx - 13, cy - 11, cx + 13, cy + 11], 165, 335, fill=IRON[5])
    d.ellipse([cx - 6, cy - 5, cx + 6, cy + 5], fill=DATA[3])
    d.point((cx, cy), fill=DATA[5])
    wx0, wy0, wx1, wy1 = wall
    led(d, wx0 + 6, wy0 + 5, DATA, 2)
    return img
