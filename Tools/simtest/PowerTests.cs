using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    public static class PowerTests
    {
        public static void Run(string repoRoot)
        {
            Check.Section("Power");
            var sim = EntityTests.NewSim();
            var pod = sim.Place("crashpod", new TilePos(-1, -1), Dir.North) as GeneratorBuilding;   // covers -1..1
            Check.True(pod != null, "pod is a generator");
            // drill far away, no wire: unpowered
            var drill = sim.Place("drill", new TilePos(5, -1), Dir.East);
            sim.Step();
            Check.Eq(0, drill.PowerSatisfaction, "drill without wire has no power");
            Check.Eq(BuildingState.NoPower, drill.State, "state NoPower");
            // wires from pod's east side (x=2) to drill's west side (x=4): tiles (2,0),(3,0),(4,0)
            for (int x = 2; x <= 4; x++) Check.True(sim.Place("wire", new TilePos(x, 0), Dir.East) != null, "wire placed at " + x);
            sim.Step();
            Check.True(drill.NetworkId != 0 && drill.NetworkId == pod.NetworkId, "pod and drill share a network");
            Check.Eq(1000, drill.PowerSatisfaction, "drill fully powered (200 kW vs 30 kW)");
            var net = sim.NetworkOf(drill);
            Check.Eq(200000L, net.Supply, "supply = pod output");
            Check.True(net.Demand >= 30000, "demand includes drill");
            // remove a wire -> network splits, drill loses power
            var midWire = sim.BuildingAt(new TilePos(3, 0));
            sim.Remove(midWire, null);
            sim.Step();
            Check.Eq(0, drill.PowerSatisfaction, "cut wire -> no power");
            // put it back, destroy it instead -> same effect
            sim.Place("wire", new TilePos(3, 0), Dir.East); sim.Step();
            Check.Eq(1000, drill.PowerSatisfaction, "reconnected");
            sim.Damage(sim.BuildingAt(new TilePos(3, 0)), 9999); sim.Step();
            Check.Eq(0, drill.PowerSatisfaction, "destroyed wire does not conduct");

            Check.Section("Reactor decay");
            int start = pod.CurrentOutput();
            for (int i = 0; i < pod.Def.DecayTicks; i++) pod.Tick(sim);
            Check.Eq(start / 10, pod.CurrentOutput(), "decays to 10% floor");
            pod.Tick(sim);
            Check.Eq(start / 10, pod.CurrentOutput(), "stays at floor");

            Check.Section("Brownout");
            var sim2 = EntityTests.NewSim();
            var pod2 = sim2.Place("crashpod", new TilePos(-1, -1), Dir.North) as GeneratorBuilding;
            // 8 data centers at 150 kW = 1.2 MW demand vs 200 kW supply
            for (int i = 0; i < 8; i++) sim2.Place("datacenter_l1", new TilePos(3 + i * 5, 0), Dir.North);
            for (int x = 2; x < 3 + 8 * 5; x++) if (sim2.BuildingAt(new TilePos(x, -1)) == null) sim2.Place("wire", new TilePos(x, -1), Dir.East);
            sim2.Step();
            var n2 = sim2.NetworkOf(pod2);
            Check.True(n2 != null && n2.Satisfaction > 150 && n2.Satisfaction < 180, "satisfaction ~16%: " + (n2 == null ? -1 : n2.Satisfaction));
        }
    }
}
