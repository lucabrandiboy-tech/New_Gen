"""
Checks that every building, item, resource and terrain type the game can ask for actually has a
picture on disk, and says where each one comes from.

Priority the game uses at run time:
    your art  (Assets/Art/Sprites)   >   Claude's art (Assets/Art/Generated)   >   flat placeholder

Run from the repo root:   python3 Tools/art/check_coverage.py
Exit code 1 if anything would fall through to a plain coloured box.
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT = os.path.join(ROOT, 'Assets', 'StreamingAssets', 'Content')
OWNER = os.path.join(ROOT, 'Assets', 'Art', 'Sprites')
CLAUDE = os.path.join(ROOT, 'Assets', 'Art', 'Generated')

FRAME_SUFFIX = re.compile(r'_(\d+)f$')


def load(name):
    """Read one content file, tolerating the // comments we use for notes."""
    text = open(os.path.join(CONTENT, name)).read()
    out = []
    for line in text.split('\n'):
        in_str = False
        for i, ch in enumerate(line):
            if ch == '"' and (i == 0 or line[i - 1] != '\\'):
                in_str = not in_str
            elif ch == '/' and not in_str and line[i:i + 2] == '//':
                line = line[:i]
                break
        out.append(line)
    return json.loads('\n'.join(out))


def keys_in(folder):
    """Sprite keys a folder provides. A trailing _<N>f is a frame count, not part of the name."""
    found = {}
    if not os.path.isdir(folder):
        return found
    for dirpath, _, files in os.walk(folder):
        for f in files:
            if not f.lower().endswith('.png'):
                continue
            stem = os.path.splitext(f)[0]
            m = FRAME_SUFFIX.search(stem)
            frames = int(m.group(1)) if m and int(m.group(1)) >= 1 else 1
            key = stem[:m.start()] if frames > 1 else stem
            found[key.lower()] = (os.path.relpath(os.path.join(dirpath, f), ROOT), frames)
    return found


def main():
    buildings = load('buildings.json')['buildings']
    items = load('items.json')['items']
    resources = load('resources.json')['resources']
    terrains = load('terrain.json')['terrain']

    required = []     # (key, what it is, required?)

    for b in buildings:
        bid, kind = b['id'], b.get('kind', 'Static')
        if kind in ('Tube', 'Wire'):
            for d in ('right', 'left', 'up', 'down'):
                required.append((f'bld_{bid}_{d}', f'{bid} belt facing {d}', True))
        else:
            required.append((f'bld_{bid}_idle', f'{bid}', True))

    for it in items:
        key = 'cell_' + it['id'].replace('cell_', '') if it.get('category') == 'cell' else 'item_' + it['id']
        required.append((key, f"item {it['id']}", True))

    for r in resources:
        required.append((f"res_{r['id']}_01", f"{r['id']} ore patch", True))

    for t in terrains:
        required.append((f"tile_{t['id']}_01", f"{t['id']} terrain", True))

    for extra, what in (('bld_damaged', 'shared damage overlay'), ('bld_destroyed', 'shared rubble'),
                        ('icon_nopower', 'no-power icon'), ('icon_missing_input', 'missing-input icon'),
                        ('icon_overheat', 'overheat icon'), ('fx_bullet', 'bullet')):
        required.append((extra, what, True))

    for d in ('down', 'up', 'side'):
        required.append((f'unit_player_idle_{d}', f'player idle {d}', True))
        required.append((f'unit_player_walk_{d}', f'player walk {d}', True))

    owner, claude = keys_in(OWNER), keys_in(CLAUDE)

    missing, from_owner, from_claude = [], [], []
    for key, what, _ in required:
        k = key.lower()
        if k in owner:
            from_owner.append((key, what, owner[k]))
        elif k in claude:
            from_claude.append((key, what, claude[k]))
        else:
            missing.append((key, what))

    print(f"{len(from_owner)} from your art, {len(from_claude)} stand-ins by Claude, {len(missing)} missing\n")
    if from_owner:
        print("YOUR ART (used instead of Claude's):")
        for key, what, (path, frames) in sorted(from_owner):
            print(f"  {key:<34} {what:<28} {path}" + (f"  [{frames} frames]" if frames > 1 else ""))
        print()
    # Optional extras you have drawn that are not on the required list (working animations and so on).
    required_keys = {k.lower() for k, _, _ in required}
    extras = [(k, v) for k, v in owner.items() if k not in required_keys]
    if extras:
        print("YOUR EXTRAS (optional art the game will use if it fits):")
        for key, (path, frames) in sorted(extras):
            print(f"  {key:<34} {path}" + (f"  [{frames} frames]" if frames > 1 else ""))
        print()

    if missing:
        print("MISSING — these would draw as a plain coloured box:")
        for key, what in sorted(missing):
            print(f"  {key:<34} {what}")
        return 1
    print("Every building, item, ore patch and terrain type has a picture.")
    return 0


if __name__ == '__main__':
    sys.exit(main())
