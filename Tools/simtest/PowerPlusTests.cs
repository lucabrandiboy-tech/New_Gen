using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    /// <summary>Coal burners, wind turbines, battery banks and the belt router.</summary>
    public static class PowerPlusTests
    {
        static void Steps(Simulation sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        public static void Run(string repoRoot)
        {
            Check.Section("Coal burner");
            var sim = EntityTests.NewSim();
            var burner = sim.Place("coal_burner", new TilePos(0, 0), Dir.North) as FuelGeneratorBuilding;
            Check.True(burner != null, "coal burner is a fuel generator");
            sim.Place("wire", new TilePos(2, 0), Dir.East);
            var drill = sim.Place("drill", new TilePos(3, 0), Dir.East);
            sim.Step();
            Check.Eq(0, burner.PowerOutputNow(), "no fuel, no power");
            Check.Eq(0, drill.PowerSatisfaction, "so the drill is dead");
            Check.True(burner.Accepts("coal"), "accepts coal");
            Check.True(!burner.Accepts("iron_ore"), "refuses things that do not burn");
            burner.Fuel.Add("coal", 2);
            sim.Step();
            Check.True(burner.Burning, "lit");
            Check.Eq(180000, burner.PowerOutputNow(), "full output while alight");
            sim.Step();
            Check.Eq(1000, drill.PowerSatisfaction, "drill now powered");
            Check.Eq(1, burner.Fuel.Count("coal"), "one coal taken, one still in the hopper");
            Steps(sim, 240);                      // 2 coal x 120 ticks
            Check.True(!burner.Burning, "burnt out after both pieces of coal");
            sim.Step();
            Check.Eq(0, drill.PowerSatisfaction, "and the drill stops again");

            Check.Section("Wind turbine");
            var s2 = EntityTests.NewSim();
            var wind = s2.Place("wind_turbine", new TilePos(0, 0), Dir.North) as GeneratorBuilding;
            Check.True(wind != null, "turbine placed");
            Check.Eq(30000, wind.PowerOutputNow(), "steady output");
            Steps(s2, 5000);
            Check.Eq(30000, wind.PowerOutputNow(), "and it never decays, unlike the pod");

            Check.Section("Battery bank");
            var s3 = EntityTests.NewSim();
            var pod = s3.Place("crashpod", new TilePos(0, 0), Dir.North) as GeneratorBuilding;   // covers 0..2, 0..2
            for (int x = 0; x <= 12; x++) s3.Place("wire", new TilePos(x, 3), Dir.East);          // bus along the pod's top
            var bat = s3.Place("battery", new TilePos(9, 4), Dir.North) as BatteryBuilding;
            Check.True(bat != null && bat.Charge == 0, "battery starts empty");
            s3.Step();
            var net = s3.NetworkOf(bat);
            Check.True(net != null && net.Id == s3.NetworkOf(pod).Id, "battery is on the pod's network");
            Steps(s3, 600);
            Check.True(bat.Charge > 0, "surplus power charged it: " + bat.Charge);
            Check.Eq(bat.Charge, net.Stored, "network reports the stored energy");
            Check.Eq(1000, net.Satisfaction, "nothing is short while the pod covers everything");

            // now overload the network and check the battery covers the gap
            long chargeBefore = bat.Charge;
            var enc = s3.Place("encoder", new TilePos(0, 4), Dir.North);        //  60 kW
            var asm = s3.Place("assembler", new TilePos(2, 4), Dir.North);      //  50 kW
            var dc = s3.Place("datacenter_l1", new TilePos(5, 4), Dir.North);   // 150 kW, over the pod's 200 kW total
            Check.True(enc != null && asm != null && dc != null, "heavy consumers placed");
            s3.Step();
            Check.True(s3.NetworkOf(bat).Demand > s3.NetworkOf(bat).Supply, "demand now exceeds the pod");
            Check.True(s3.NetworkOf(bat).FromBatteries > 0, "battery is covering the shortfall");
            Check.True(bat.Charge < chargeBefore, "and draining while it does");
            Steps(s3, 4000);
            Check.Eq(0L, bat.Charge, "eventually flat");
            Check.True(s3.NetworkOf(bat).Satisfaction < 1000, "then the network browns out");

            Check.Section("Router");
            var s4 = EntityTests.NewSim();
            var burner4 = s4.Place("coal_burner", new TilePos(0, 2), Dir.North) as FuelGeneratorBuilding;  // covers 0..1, 2..3
            burner4.Fuel.Add("coal", 200);
            s4.Place("wire", new TilePos(2, 2), Dir.East);
            s4.Place("wire", new TilePos(3, 2), Dir.East);
            // one belt in from the west, two belts out north and south; side wires power the out belts
            var feed = s4.Place("tube", new TilePos(4, 2), Dir.East) as ConveyorBuilding;
            s4.Place("wire", new TilePos(4, 1), Dir.East);
            s4.Place("wire", new TilePos(4, 3), Dir.East);
            var router = s4.Place("router", new TilePos(5, 2), Dir.North) as RouterBuilding;
            var outN = s4.Place("tube", new TilePos(5, 3), Dir.North) as ConveyorBuilding;
            var outS = s4.Place("tube", new TilePos(5, 1), Dir.South) as ConveyorBuilding;
            Check.True(router != null && outN != null && outS != null, "router with two outputs placed");
            Steps(s4, 2);   // tick 1 lights the burner, tick 2 is the first with its power on the network
            Check.Eq(1000, router.PowerSatisfaction, "router powered");
            Check.Eq(1000, outN.PowerSatisfaction, "north belt powered");
            Check.Eq(1000, outS.PowerSatisfaction, "south belt powered");

            for (int i = 0; i < 12; i++) { feed.TryInsert("iron_ore"); Steps(s4, 20); }
            Steps(s4, 600);
            int north = outN.Items.Count, south = outS.Items.Count;
            Check.True(north > 0 && south > 0, $"items went both ways (north {north}, south {south})");
            Check.True(Fix.Abs(north - south) <= 2, $"and roughly evenly ({north} vs {south})");
            Check.True(router.Items.Count <= 4, "router never holds more than its slots");

            // a router whose only usable output is a chest sends everything there
            var s5 = EntityTests.NewSim();
            var b5 = s5.Place("coal_burner", new TilePos(0, 2), Dir.North) as FuelGeneratorBuilding;
            b5.Fuel.Add("coal", 200);
            s5.Place("wire", new TilePos(2, 2), Dir.East);
            s5.Place("wire", new TilePos(3, 2), Dir.East);
            var feed5 = s5.Place("tube", new TilePos(4, 2), Dir.East) as ConveyorBuilding;
            var router5 = s5.Place("router", new TilePos(5, 2), Dir.North) as RouterBuilding;
            var only = s5.Place("chest", new TilePos(5, 1), Dir.North) as ChestBuilding;
            Steps(s5, 2);
            Check.Eq(1000, router5.PowerSatisfaction, "second router powered");
            for (int i = 0; i < 6; i++) { feed5.TryInsert("coal"); Steps(s5, 20); }
            Steps(s5, 300);
            Check.Eq(6, only.Storage.Count("coal"), "everything went to the only output that would take it");
            Check.Eq(0, router5.Items.Count, "router emptied");
        }
    }
}
