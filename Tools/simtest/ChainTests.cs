using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    /// <summary>
    /// The whole Milestone 1 loop in one go: a powered line of machines turning iron ore into
    /// data cells and finishing a research. If this passes, the core game works end to end.
    ///
    /// Layout, all on row y = 1, with one long power wire along row y = 0:
    ///   0..2 pod | 4 chest(ore) | 5 arm | 6..7 smelter | 8 arm | 9 chest(plates) | 10 arm
    ///   | 11..13 assembler | 14 arm | 15 chest(circuits) | 16 arm | 17..18 encoder
    ///   | 19 arm | 20..23 data center
    /// Each arm is placed facing east, which wires it west-in / east-out by default.
    /// </summary>
    public static class ChainTests
    {
        public static void Run(string repoRoot)
        {
            Check.Section("Full production chain");
            var sim = EntityTests.NewSim(4242);

            // power: pod plus a wire running under the whole line
            var pod = sim.Place("crashpod", new TilePos(0, 1), Dir.North);
            Check.True(pod != null, "pod placed");
            for (int x = 0; x <= 24; x++) sim.Place("wire", new TilePos(x, 0), Dir.East);

            var oreChest = sim.Place("chest", new TilePos(4, 1), Dir.North) as ChestBuilding;
            sim.Place("arm", new TilePos(5, 1), Dir.East);
            var smelter = sim.Place("smelter", new TilePos(6, 1), Dir.North) as CrafterBuilding;
            sim.Place("arm", new TilePos(8, 1), Dir.East);
            var plateChest = sim.Place("chest", new TilePos(9, 1), Dir.North) as ChestBuilding;
            sim.Place("arm", new TilePos(10, 1), Dir.East);
            var assembler = sim.Place("assembler", new TilePos(11, 1), Dir.North) as CrafterBuilding;
            sim.Place("arm", new TilePos(14, 1), Dir.East);
            var circuitChest = sim.Place("chest", new TilePos(15, 1), Dir.North) as ChestBuilding;
            sim.Place("arm", new TilePos(16, 1), Dir.East);
            var encoder = sim.Place("encoder", new TilePos(17, 1), Dir.North) as CrafterBuilding;
            sim.Place("arm", new TilePos(19, 1), Dir.East);
            var dc = sim.Place("datacenter_l1", new TilePos(20, 1), Dir.North) as DataCenterBuilding;

            Check.True(oreChest != null && smelter != null && plateChest != null, "first half of the line placed");
            Check.True(assembler != null && circuitChest != null && encoder != null && dc != null, "second half placed");

            // every machine should be on the pod's network
            sim.Step();
            Check.True(smelter.NetworkId != 0 && smelter.NetworkId == dc.NetworkId, "one shared power network");
            var net = sim.NetworkOf(smelter);
            Check.True(net.Demand > net.Supply, "this line deliberately out-draws the dying pod, so everything runs slow");

            // choose what each machine makes
            smelter.SetRecipe(sim, "iron_plate", null);
            assembler.SetRecipe(sim, "circuit", null);
            encoder.SetRecipe(sim, "cell_resource", null);
            Check.Eq("iron_plate", smelter.RecipeId, "smelter set");

            // Raw inputs the line cannot make for itself yet. The tech needs 100 cells; each cell is
            // 2 circuits + 1 coal, and each circuit is 1 iron plate + 1 copper plate — so 200 of each
            // plate and 100 coal are the minimum, and these are comfortably above that.
            oreChest.Storage.Add("iron_ore", 300);
            plateChest.Storage.Add("copper_plate", 300);
            circuitChest.Storage.Add("coal", 150);

            Check.True(sim.Research.Enqueue("first_contact"), "research queued");

            // part way through, every stage should have something moving through it
            for (int i = 0; i < 6000; i++) sim.Step();
            Check.True(oreChest.Storage.Count("iron_ore") < 300, "ore is being drawn out of the first chest");
            int ironSomewhere = smelter.Outputs.Count("iron_plate") + plateChest.Storage.Count("iron_plate")
                              + assembler.Inputs.Count("iron_plate");
            Check.True(ironSomewhere > 0, "the smelter is producing iron plates");
            int circuitsSomewhere = assembler.Outputs.Count("circuit") + circuitChest.Storage.Count("circuit")
                                  + encoder.Inputs.Count("circuit");
            Check.True(circuitsSomewhere > 0, "the assembler is producing circuits");

            // then let it run to the end
            for (int i = 0; i < 80000 && !sim.Research.Unlocked.Contains("first_contact"); i++) sim.Step();
            Check.True(sim.Research.Unlocked.Contains("first_contact"),
                       "research finished from cells the factory made itself, progress was "
                       + sim.Research.ProgressOfCurrent1000() / 10 + "%");
            Check.True(!dc.Overheated, "the data center stayed cool on snow");
            Check.True(sim.Research.Queue.Count == 0, "queue emptied");

            Check.Section("Bootstrap from nothing");
            // A fresh player must be able to reach the first smelter with only hand-mined stone.
            var s2 = EntityTests.NewSim();
            s2.StartNewGame();
            var p = s2.LocalPlayer;
            Check.Eq(PlaceResult.MissingItems, s2.CanPlace("smelter", new TilePos(6, 6), Dir.North, p.Inventory),
                     "cannot afford a smelter with an empty inventory");
            p.Inventory.Add("stone", 12);
            Check.Eq(PlaceResult.Ok, s2.CanPlace("smelter", new TilePos(6, 6), Dir.North, p.Inventory),
                     "12 hand-mined stone is enough for the first smelter");
            Check.True(s2.CanHandCraft(p, "hand_iron_plate", out _) == false, "no ore yet, so no hand plate");
            p.Inventory.Add("iron_ore", 3);
            Check.True(s2.CanHandCraft(p, "hand_iron_plate", out _), "with ore, a plate can be made by hand");
            s2.QueueHandCraft(p, "hand_iron_plate", 1);
            for (int i = 0; i < 121; i++) s2.Step();
            Check.Eq(1, p.Inventory.Count("iron_plate"), "hand plate done after 120 ticks");
            Check.Eq(2, p.Inventory.Count("iron_ore"), "one ore consumed");
        }
    }
}
