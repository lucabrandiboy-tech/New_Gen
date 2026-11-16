"""
Writes every Claude-made sprite into Assets/Art/Generated/.

These are STAND-INS. Anything the owner draws into Assets/Art/Sprites/ with the same name wins
automatically (priority: owner art > generated > flat placeholder). The owner is drawing the
buildings, the UI and the faction-leader call screens; everything here that is a building is only
there so the game looks finished until those arrive.

Run from the repo root:   python3 Tools/art/generate_all.py Assets/Art/Generated
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from palette import *
import sprites_small as S
import sprites_buildings as B
import sprites_units as U
from PIL import Image

def strip(frames):
    """Lay frames out left to right in one row."""
    w, h = frames[0].size
    img = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        img.alpha_composite(f, (i * w, 0))
    return img

def build(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    art = {}

    # ---- items, cells, ore, icons, effects
    art['item_iron_ore'] = S.ore_item(IRON)
    art['item_copper_ore'] = S.ore_item(COPPER)
    art['item_coal'] = S.ore_item(COAL)
    art['item_stone'] = S.ore_item(STONE)
    art['item_ice'] = S.ice_item()
    art['item_iron_plate'] = S.plate_item(IRON)
    art['item_copper_plate'] = S.plate_item(COPPER)
    art['item_circuit'] = S.circuit_item()
    art['cell_resource'] = S.data_cell()
    art['res_iron_01'] = S.ore_patch(IRON, 1)
    art['res_copper_01'] = S.ore_patch(COPPER, 2)
    art['res_coal_01'] = S.ore_patch(COAL, 3)
    art['res_stone_01'] = S.ore_patch(STONE, 4)
    art['icon_nopower'] = S.icon_nopower()
    art['icon_missing_input'] = S.icon_missing_input()
    art['icon_overheat'] = S.icon_overheat()
    art['fx_bullet'] = S.bullet()
    art['bld_damaged'] = S.damaged_overlay()
    art['bld_destroyed'] = S.destroyed_tile()

    # ---- player
    for d in ('down', 'up', 'side'):
        art[f'unit_player_idle_{d}'] = U.player(d, 0.0)
        art[f'unit_player_walk_{d}_4f'] = U.player_walk_sheet(d, 4)

    # ---- belts and data wires: one drawing per direction, 4 frames each
    for d in ('right', 'left', 'up', 'down'):
        art[f'bld_tube_{d}_4f'] = U.belt_sheet(d, 4)
        art[f'bld_wire_{d}_4f'] = U.wire_sheet(d, 4)

    # ---- buildings (stand-ins until the owner's versions land)
    art['bld_crashpod_idle'] = B.crashpod(1.0)
    art['bld_chest_idle'] = B.chest()
    art['bld_arm_idle'] = B.arm(0.0)

    art['bld_drill_idle'] = B.drill(0.0, None, False)
    for side in ('right', 'left', 'up', 'down'):
        art[f'bld_drill_working_{side}_3f'] = strip([
            B.drill(i * 0.7, side, True) for i in range(3)])

    art['bld_smelter_idle'] = B.smelter(0.0)
    # 10 frames stretched across the craft: heat climbs, then the pour at the end
    art['bld_smelter_working_10f'] = strip([B.smelter(min(1.0, (i + 1) / 9)) for i in range(10)])

    art['bld_assembler_idle'] = B.assembler(0.0, False)
    art['bld_assembler_working_4f'] = strip([B.assembler(i * 1.57, True) for i in range(4)])

    art['bld_encoder_idle'] = B.encoder(0.0, False)
    art['bld_encoder_working_4f'] = strip([B.encoder(i / 4, True) for i in range(4)])

    art['bld_datacenter_l1_idle'] = B.datacenter(0.0, False, 0.0)
    art['bld_datacenter_l1_working_4f'] = strip([B.datacenter(i / 4, True, 0.3) for i in range(4)])

    for name, img in art.items():
        img.save(os.path.join(out_dir, name + '.png'))
    return art

if __name__ == '__main__':
    target = sys.argv[1] if len(sys.argv) > 1 else 'Assets/Art/Generated'
    made = build(target)
    print(f'{len(made)} sprites written to {target}')
