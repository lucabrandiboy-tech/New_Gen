using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    public static class LogisticsTests
    {
        static void Steps(Simulation sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        public static void Run(string repoRoot)
        {
            Check.Section("Tubes");
            var sim = EntityTests.NewSim();
            sim.Place("crashpod", new TilePos(-1, -1), Dir.North);
            // tube line eastward from (2,0) to (6,0), touching the pod's east side -> powered
            var tubes = new ConveyorBuilding[5];
            for (int i = 0; i < 5; i++) tubes[i] = sim.Place("tube", new TilePos(2 + i, 0), Dir.East) as ConveyorBuilding;
            Check.True(tubes[0] != null, "tube is a conveyor");
            sim.Step();
            Check.Eq(1000, tubes[4].PowerSatisfaction, "tube line powered through the chain");
            Check.True(tubes[1].FeedFrom == Dir.West, "straight feed from behind");
            Check.True(tubes[0].TryInsert("iron_ore"), "insert at start");
            Check.True(!tubes[0].TryInsert("iron_ore"), "no room right behind it");
            Steps(sim, 8);
            Check.True(tubes[0].Items.Count == 0 && tubes[1].Items.Count == 1, "item moved to the next tube after 8 ticks");
            Steps(sim, 40);
            Check.True(tubes[4].Items.Count == 1 && tubes[4].Items[0].Pos == ConveyorBuilding.Exit, "item waits at the end of the line");
            // fill the line
            for (int t = 0; t < 60; t++) { tubes[0].TryInsert("coal"); sim.Step(); }
            int total = 0; foreach (var tb in tubes) total += tb.Items.Count;
            Check.True(total >= 18 && total <= 20, "line holds ~4 items per tile: " + total);
            // cut power: items stop
            var cutPod = sim.BuildingAt(new TilePos(0, 0));
            sim.Damage(cutPod, 99999);
            sim.Step();
            int posBefore = tubes[2].Items[0].Pos; sim.Step();
            Check.Eq(posBefore, tubes[2].Items[0].Pos, "items hold when unpowered");
            Check.True(!tubes[0].CarriesItem(sim.Db, "cell_resource"), "tubes refuse data cells");

            Check.Section("Corners");
            var s2 = EntityTests.NewSim();
            var w1 = s2.Place("wire", new TilePos(0, 0), Dir.East) as ConveyorBuilding;
            var w2 = s2.Place("wire", new TilePos(1, 0), Dir.North) as ConveyorBuilding;
            var w3 = s2.Place("wire", new TilePos(1, 1), Dir.North) as ConveyorBuilding;
            Check.True(w2.FeedFrom == Dir.West, "corner fed from the west side");
            Check.True(w3.FeedFrom == Dir.South, "straight after corner");
            w1.TryInsert("cell_resource");
            Steps(s2, 30);
            Check.True(w3.Items.Count == 1, "cell travelled around the corner without power");
            Check.True(!w1.CarriesItem(s2.Db, "iron_ore"), "wires refuse ore");
            // head-on tubes do not connect
            var s3 = EntityTests.NewSim();
            var a = s3.Place("wire", new TilePos(0, 0), Dir.East) as ConveyorBuilding;
            var b = s3.Place("wire", new TilePos(1, 0), Dir.West) as ConveyorBuilding;
            a.TryInsert("cell_resource"); Steps(s3, 30);
            Check.True(a.Items.Count == 1 && b.Items.Count == 0, "head-on conveyors do not pass items");

            Check.Section("Arms");
            var s4 = EntityTests.NewSim();
            s4.Place("crashpod", new TilePos(-1, -1), Dir.North);
            var chestA = s4.Place("chest", new TilePos(3, 0), Dir.North) as ChestBuilding;
            var arm = s4.Place("arm", new TilePos(4, 0), Dir.East) as ArmBuilding;   // pulls from (3,0), drops at (5,0)
            var tube = s4.Place("tube", new TilePos(5, 0), Dir.East) as ConveyorBuilding;
            var arm2 = s4.Place("arm", new TilePos(6, 0), Dir.East) as ArmBuilding;   // tube -> chestB
            var chestB = s4.Place("chest", new TilePos(7, 0), Dir.North) as ChestBuilding;
            s4.Place("wire", new TilePos(2, 0), Dir.East);                              // pod -> wire -> chestA? chest not a conductor...
            // wire touches arm (4,0)? no: (2,0) touches (3,0) chest only. Add wire along y=1 to reach arms and tube.
            for (int x = 2; x <= 6; x++) s4.Place("wire", new TilePos(x, 1), Dir.East);
            s4.Step();
            Check.Eq(1000, arm.PowerSatisfaction, "arm powered via wire above it");
            chestA.Storage.Add("iron_ore", 5);
            Steps(s4, 400);
            Check.Eq(0, chestA.Storage.Count("iron_ore"), "arm emptied chest A");
            Check.Eq(5, chestB.Storage.Count("iron_ore"), "all 5 ore arrived in chest B");
            Check.True(arm.Holding == null && tube.Items.Count == 0, "nothing left in transit");
        }
    }
}
