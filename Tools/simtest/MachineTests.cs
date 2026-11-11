using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    public static class MachineTests
    {
        static void Steps(Simulation sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        static TilePos FindPatch(Simulation sim, byte res)
        {
            for (int r = 8; r < 40; r++)
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                    {
                        // need a 3x3 fully on the resource
                        bool ok = true;
                        for (int dy = 0; dy < 3 && ok; dy++) for (int dx = 0; dx < 3; dx++) if (sim.World.GetTile(new TilePos(x + dx, y + dy)).Resource != res) { ok = false; break; }
                        if (ok) return new TilePos(x, y);
                    }
            return new TilePos(int.MinValue, 0);
        }

        public static void Run(string repoRoot)
        {
            Check.Section("Drill");
            var sim = EntityTests.NewSim();
            var patch = FindPatch(sim, 1); // iron
            Check.True(patch.X != int.MinValue, "found a 3x3 iron spot");
            var drill = sim.Place("drill", patch, Dir.East) as DrillBuilding;
            Check.True(drill != null, "drill placed on iron");
            Check.True(drill.HasAnythingToMine(sim), "drill sees iron");
            var chest = sim.Place("chest", drill.FrontTile(), Dir.North) as ChestBuilding;
            // power: wire chain from a pod placed next to the drill
            var podPos = new TilePos(patch.X - 4, patch.Y);
            var pod = sim.Place("crashpod", podPos, Dir.North);
            Check.True(pod != null, "pod placed near drill");
            // pod covers x-4..x-2; drill starts at x. Wire at x-1 touches both.
            Check.True(sim.Place("wire", new TilePos(patch.X - 1, patch.Y), Dir.East) != null, "wire between pod and drill");
            Steps(sim, 1);
            Check.Eq(1000, drill.PowerSatisfaction, "drill powered");
            Steps(sim, 60 * 3 + 5);
            Check.Eq(3, chest.Storage.Count("iron_ore"), "3 ore mined in 3x60 ticks");
            Check.Eq(BuildingState.Working, drill.State, "drill shows working");
            // block the output: fill the chest with something else
            chest.Storage.Clear(); for (int i = 0; i < chest.Storage.SlotCount; i++) chest.Storage.Add("coal", 50);
            Steps(sim, 200);
            Check.True(drill.Buffered != null, "drill holds an item when output is blocked");
            Check.Eq(BuildingState.Idle, drill.State, "blocked drill is idle");

            Check.Section("Crafter");
            var db = ContentTests.Db;
            var tmp = new RecipeDef { Id = "__test_plate", Name = "t", Crafter = "smelter", Ticks = 10 };
            tmp.Inputs.Add(new ItemStack("iron_ore", 2)); tmp.Outputs.Add(new ItemStack("iron_plate", 1));
            db.Recipes[tmp.Id] = tmp;
            try
            {
                var s2 = EntityTests.NewSim();
                s2.Place("crashpod", new TilePos(-1, -1), Dir.North);
                s2.Place("wire", new TilePos(2, 0), Dir.East);
                var sm = s2.Place("smelter", new TilePos(3, 0), Dir.North) as CrafterBuilding;
                Check.True(sm != null, "smelter is a crafter");
                Check.True(!sm.Accepts("iron_ore"), "no recipe -> accepts nothing");
                Check.Eq(1, s2.AvailableRecipes("smelter").Count, "test recipe available to smelters");
                Check.Eq(0, s2.AvailableRecipes("assembler").Count, "not to assemblers");
                sm.SetRecipe(s2, tmp.Id, null);
                Check.True(sm.Accepts("iron_ore") && !sm.Accepts("coal"), "accepts only recipe inputs");
                sm.Inputs.Add("iron_ore", 5);
                Steps(s2, 1);
                Check.Eq(3, sm.Inputs.Count("iron_ore"), "inputs consumed at craft start");
                Steps(s2, 10);
                Check.Eq(1, sm.Outputs.Count("iron_plate"), "plate produced after 10 ticks");
                Steps(s2, 12);
                Check.Eq(2, sm.Outputs.Count("iron_plate"), "second plate");
                Steps(s2, 30);
                Check.True(sm.MissingInput, "flags missing input with 1 ore left");
                var bag = new Inventory(db, 5);
                sm.SetRecipe(s2, null, bag);
                Check.True(bag.Count("iron_plate") == 2 && bag.Count("iron_ore") == 1, "changing recipe returns contents");
            }
            finally { db.Recipes.Remove(tmp.Id); }

            Check.Section("Research");
            var s3 = EntityTests.NewSim();
            s3.Place("crashpod", new TilePos(-1, -1), Dir.North);
            s3.Place("wire", new TilePos(2, 0), Dir.East);
            var dc = s3.Place("datacenter_l1", new TilePos(3, 0), Dir.North) as DataCenterBuilding;
            Check.True(dc != null, "data center placed");
            Check.True(dc.Accepts("cell_resource") && !dc.Accepts("iron_ore"), "accepts cells only");
            Check.True(s3.Research.Enqueue("first_contact"), "queued placeholder tech");
            Check.True(!s3.Research.Enqueue("first_contact"), "cannot queue twice");
            Steps(s3, 5);
            Check.True(dc.MissingCells, "stalls without cells");
            dc.Cells.Add("cell_resource", 100);
            Steps(s3, 110);
            Check.True(s3.Research.Unlocked.Contains("first_contact"), "tech finished after ~100 ticks with 100 cells");
            Check.Eq(0, dc.Cells.Count("cell_resource"), "all 100 cells consumed");
            Check.True(!dc.Overheated && dc.Heat < dc.Def.MaxHeat * 1000, "never overheated on snow");
            Check.Eq(0, s3.Research.Queue.Count, "queue empty");
            bool gotEvent = false; foreach (var e in s3.Events) if (e.Type == SimEventType.ResearchFinished) gotEvent = true;
            Check.True(gotEvent, "ResearchFinished event");
        }
    }
}
