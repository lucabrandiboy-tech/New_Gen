using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using NewGen.Sim.Save;

namespace NewGen.SimTest
{
    public static class SaveTests
    {
        static void Steps(Simulation sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        public static void Run(string repoRoot)
        {
            Check.Section("Save/load");
            var db = ContentTests.Db;
            var sim = EntityTests.NewSim(99);
            sim.StartNewGame();
            var p = sim.LocalPlayer; p.Input = new PlayerInput { Aim = p.Pos };
            p.Inventory.Add("coal", 12);
            // a little factory: pod -> wire -> tube line with an item, arm, chest, drill on the starter iron patch
            for (int x = 2; x <= 5; x++) sim.Place("wire", new TilePos(x, 0), Dir.East);
            var tube = sim.Place("tube", new TilePos(2, 1), Dir.North) as ConveyorBuilding; tube.TryInsert("iron_ore", 100);
            var chest = sim.Place("chest", new TilePos(6, 0), Dir.North) as ChestBuilding; chest.Storage.Add("stone", 3);
            var drill = sim.Place("drill", new TilePos(-1, 15), Dir.South) as DrillBuilding;   // starter iron ring is north
            sim.Place("chest", drill.FrontTile(), Dir.North);
            sim.World.SetTile(new TilePos(40, 40), new NewGen.Sim.World.Tile(2, 0));   // a modified tile
            sim.Research.Enqueue("first_contact");
            Steps(sim, 50);

            byte[] data = SaveSystem.Write(sim, manual: true);
            Check.True(data.Length < 20000, "save is small: " + data.Length + " bytes");
            Check.Eq(sim.Tick, sim.LastManualSaveTick, "manual save stamps the tick");
            Check.True(!SaveSystem.CanManualSave(sim, out int left) && left > 590, "10-minute cooldown active: " + left);

            var sim2 = SaveSystem.Read(data, db);
            Check.Eq(sim.Tick, sim2.Tick, "tick restored");
            Check.Eq(sim.Seed, sim2.Seed, "seed restored");
            Check.Eq(sim.Buildings.Count, sim2.Buildings.Count, "building count");
            Check.Eq(sim.LocalPlayer.Pos, sim2.LocalPlayer.Pos, "player position");
            Check.Eq(12, sim2.LocalPlayer.Inventory.Count("coal"), "player inventory");
            var chest2 = sim2.BuildingAt(new TilePos(6, 0)) as ChestBuilding;
            Check.Eq(3, chest2.Storage.Count("stone"), "chest contents");
            var tube2 = sim2.BuildingAt(new TilePos(2, 1)) as ConveyorBuilding;
            Check.Eq(tube.Items.Count, tube2.Items.Count, "tube items");
            Check.True(sim2.World.GetTile(new TilePos(40, 40)).Terrain == 2, "tile delta restored");
            Check.Eq(1, sim2.Research.Queue.Count, "research queue restored");
            Check.Eq(sim.LastManualSaveTick, sim2.LastManualSaveTick, "cooldown survives reload");

            // determinism: both sims continue identically
            sim2.LocalPlayer.Input = sim.LocalPlayer.Input;
            Steps(sim, 200); Steps(sim2, 200);
            Check.Eq(SaveSystem.StateHash(sim), SaveSystem.StateHash(sim2), "identical after 200 more ticks");

            // ids keep growing past the loaded ones
            var extra = sim2.Place("chest", new TilePos(10, 10), Dir.North);
            Check.True(extra.Id > sim.NextEntityId - 2, "new ids do not collide");

            Check.Section("Ironman");
            var im = EntityTests.NewSim(); im.StartNewGame(); im.Settings.Ironman = true;
            Check.True(!SaveSystem.AutosaveAllowed(im), "no autosave in ironman");
            SaveSystem.Write(im, true);
            SaveSystem.CanManualSave(im, out int l2);
            Check.True(l2 > 3500, "1-hour cooldown in ironman: " + l2);
            var bad = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            Check.Throws(() => SaveSystem.Read(bad, db), "garbage is rejected");
        }
    }
}
