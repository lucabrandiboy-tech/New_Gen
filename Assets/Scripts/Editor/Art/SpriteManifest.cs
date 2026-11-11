using System.Collections.Generic;
using NewGen.Art;
using UnityEngine;

namespace NewGen.EditorTools.Art
{
    /// <summary>
    /// Every sprite the game currently expects, with exact sizes.
    /// The placeholder generator makes one flat-colour PNG per entry.
    /// Must stay in sync with the sprite list in Docs/ART_GUIDE.md.
    ///
    /// STATUS: APPROVED by owner 2026-11-09.
    /// </summary>
    public static class SpriteManifest
    {
        public enum Kind { Terrain, Resource, Item, Cell, Unit, Building, Logistics }

        public readonly struct Spec
        {
            public readonly string name;      // file name without frame suffix / extension
            public readonly Kind kind;
            public readonly int frameWidth;   // pixels
            public readonly int frameHeight;  // pixels
            public readonly int frames;       // 1 = single sprite
            public readonly int footprintW;   // tiles (buildings only, else 0)
            public readonly int footprintH;

            public Spec(string name, Kind kind, int w, int h, int frames = 1, int footW = 0, int footH = 0)
            {
                this.name = name; this.kind = kind;
                frameWidth = w; frameHeight = h; this.frames = frames;
                footprintW = footW; footprintH = footH;
            }

            public string FileName => frames > 1 ? $"{name}_{frames}f.png" : $"{name}.png";
        }

        const int T = SpriteNaming.PixelsPerUnit; // one tile (64 px)
        const int RoofOverhang = T / 2; // extra height above the footprint for 3/4 view (PROPOSED)
        static readonly string[] BuildingStates = { "idle", "damaged", "destroyed" };

        public static IReadOnlyList<Spec> All => all ??= Build();
        static List<Spec> all;

        static List<Spec> Build()
        {
            var list = new List<Spec>();

            // Terrain (1 tile, must tile seamlessly)
            foreach (string n in new[] { "tile_snow_01", "tile_ice_01", "tile_rock_01" })
                list.Add(new Spec(n, Kind.Terrain, T, T));

            // Resource patches drawn on top of terrain
            foreach (string n in new[] { "res_iron_01", "res_copper_01", "res_coal_01", "res_stone_01" })
                list.Add(new Spec(n, Kind.Resource, T, T));

            // Item icons (inventory + items flying in tubes)
            foreach (string n in new[] { "item_iron_ore", "item_copper_ore", "item_coal", "item_stone", "item_ice", "item_iron_plate", "item_copper_plate" })
                list.Add(new Spec(n, Kind.Item, T / 2, T / 2));

            // Data cells
            list.Add(new Spec("cell_resource", Kind.Cell, T / 2, T / 2));

            // Player (pre-upload human body). Side views are mirrored in code for left/right.
            foreach (string dir in new[] { "down", "up", "side" })
            {
                list.Add(new Spec($"unit_player_idle_{dir}", Kind.Unit, T, T));
                list.Add(new Spec($"unit_player_walk_{dir}", Kind.Unit, T, T, frames: 4));
            }

            // Buildings: footprint in tiles. Sprite = footprint width x (footprint height + overhang).
            AddBuilding(list, "crashpod", 3, 3);
            AddBuilding(list, "drill", 3, 3);
            // Drill working animation (owner-drawn), one per output side: frames 1,2,3 looped, 0.10 s per frame.
            foreach (string side in new[] { "right", "left", "up", "down" })
                list.Add(new Spec($"bld_drill_working_{side}", Kind.Building, 3 * T, 3 * T + RoofOverhang, frames: 3, footW: 3, footH: 3));
            // Drill with no power (owner-drawn). A flashing "no power" icon will be drawn on top later.
            list.Add(new Spec("bld_drill_nopower", Kind.Building, 3 * T, 3 * T + RoofOverhang, footW: 3, footH: 3));
            AddBuilding(list, "smelter", 2, 2);
            AddBuilding(list, "assembler", 3, 3);
            AddBuilding(list, "encoder", 2, 2);
            AddBuilding(list, "datacenter_l1", 4, 4);
            AddBuilding(list, "chest", 1, 1);

            // Logistics pieces: 1 tile, flat (no overhang), rotated in code.
            foreach (string n in new[] { "bld_tube_straight", "bld_tube_corner", "bld_wire_straight", "bld_wire_corner", "bld_arm" })
                list.Add(new Spec(n, Kind.Logistics, T, T, footW: 1, footH: 1));

            return list;
        }

        static void AddBuilding(List<Spec> list, string name, int w, int h)
        {
            foreach (string state in BuildingStates)
                list.Add(new Spec($"bld_{name}_{state}", Kind.Building, w * T, h * T + RoofOverhang, footW: w, footH: h));
        }

        public static Color32 BaseColor(Kind kind)
        {
            switch (kind)
            {
                case Kind.Terrain:   return new Color32(200, 214, 228, 255); // pale ice-blue
                case Kind.Resource:  return new Color32(150, 110, 70, 255);  // brown
                case Kind.Item:      return new Color32(230, 190, 60, 255);  // yellow
                case Kind.Cell:      return new Color32(60, 210, 220, 255);  // cyan
                case Kind.Unit:      return new Color32(210, 70, 70, 255);   // red
                case Kind.Building:  return new Color32(110, 120, 140, 255); // steel grey-blue
                case Kind.Logistics: return new Color32(150, 90, 200, 255);  // purple
                default:             return new Color32(255, 0, 255, 255);   // magenta = unknown
            }
        }
    }
}
