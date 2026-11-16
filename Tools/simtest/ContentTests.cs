using System;
using System.IO;
using NewGen.Sim.Content;

namespace NewGen.SimTest
{
    public static class ContentTests
    {
        public static ContentDatabase Db;

        public static void Run(string repoRoot)
        {
            Check.Section("Content");
            string dir = Path.Combine(repoRoot, "Assets", "StreamingAssets", "Content");
            Db = ContentDatabase.LoadFromDirectory(dir);
            Check.Eq(0, Db.Problems.Count, "content has no validation problems: " + string.Join("; ", Db.Problems));
            Check.Eq(4, Db.Terrains.Count, "4 terrains");
            Check.Eq("ice", Db.TerrainById("ice").YieldsItem, "ice terrain yields ice");
            Check.Eq("snow", Db.Terrain(0).Id, "terrain 0 is snow");
            Check.Eq(5, Db.Resources.Count, "4 resources + none");
            Check.True(Db.Items.ContainsKey("iron_ore"), "iron_ore item");
            Check.True(Db.Item("cell_resource").IsDataCell, "cell category");
            Check.Eq("cell_resource", Db.Item("cell_resource").SpriteKey, "cell sprite key");
            Check.Eq("item_iron_ore", Db.Item("iron_ore").SpriteKey, "item sprite key");
            Check.Eq(BuildingKind.Drill, Db.Building("drill").Kind, "drill kind");
            Check.Eq(3, Db.Building("drill").Width, "drill is 3 wide");
            Check.Eq(4, Db.Building("datacenter_l1").Height, "datacenter 4 tall");
            Check.Eq(30, Db.Balance.TicksPerSecond, "balance loaded");
            Check.Eq(6, Db.Recipes.Count, "6 recipes");
            Check.Eq("smelter", Db.Recipe("iron_plate").Crafter, "iron plate is smelted");
            Check.Eq(10, Db.Recipe("iron_plate").Ticks, "smelt takes 10 ticks = the 10 animation frames");
            Check.Eq(120, Db.Recipe("hand_iron_plate").Ticks, "the hand version is 12x slower");
            Check.Eq("assembler", Db.Recipe("circuit").Crafter, "circuits are assembled");
            Check.Eq("encoder", Db.Recipe("cell_resource").Crafter, "cells are encoded");
            Check.True(Db.Item("circuit") != null, "circuit item exists");
            Check.Eq(12, Db.Building("smelter").Cost[0].Count, "first smelter costs 12 stone only");
            Check.Eq("stone", Db.Building("smelter").Cost[0].Item, "...and nothing else, so it bootstraps");
            Check.Eq(1, Db.Techs.Count, "placeholder tech");
            Check.Eq("No power", Db.Text("alert.no_power", "?"), "loc lookup");
            Check.Eq("fallback", Db.Text("missing.key", "fallback"), "loc fallback");
        }
    }
}
