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
            Check.Eq(3, Db.Terrains.Count, "3 terrains");
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
            Check.Eq(0, Db.Recipes.Count, "no recipes yet (owner decision)");
            Check.Eq(1, Db.Techs.Count, "placeholder tech");
            Check.Eq("No power", Db.Text("alert.no_power", "?"), "loc lookup");
            Check.Eq("fallback", Db.Text("missing.key", "fallback"), "loc fallback");
        }
    }
}
