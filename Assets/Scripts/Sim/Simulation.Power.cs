using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using NewGen.Sim.Systems;

namespace NewGen.Sim
{
    /// <summary>
    /// Power rules (M1):
    ///  - Wires and tubes are CONDUCTORS: they join each other when side-by-side.
    ///  - Any other building (generator or consumer) joins a network when its footprint touches a conductor.
    ///    Buildings do not pass power to each other directly; you need a wire or tube between them.
    ///  - Each tick, satisfaction = supply / demand (capped at 100%). Machines run at that speed.
    /// Networks are rebuilt with a flood fill whenever a building is placed or removed.
    /// </summary>
    public sealed partial class Simulation
    {
        public readonly Dictionary<int, PowerNetwork> Networks = new Dictionary<int, PowerNetwork>();
        bool networksDirty = true;
        int nextNetworkId = 1;

        public static bool IsConductor(Building b) => b != null && !b.IsDestroyed && b.Def.IsConveyor;

        public void MarkNetworksDirty() => networksDirty = true;

        public PowerNetwork NetworkOf(Building b) => b != null && b.NetworkId != 0 && Networks.TryGetValue(b.NetworkId, out var n) ? n : null;

        void RebuildNetworks()
        {
            networksDirty = false;
            Networks.Clear();
            foreach (var b in buildings) b.NetworkId = 0;

            var stack = new Stack<Building>();
            foreach (var seed in buildings)
            {
                if (!IsConductor(seed) || seed.NetworkId != 0) continue;
                var net = new PowerNetwork { Id = nextNetworkId++ };
                Networks[net.Id] = net;
                seed.NetworkId = net.Id; net.Members.Add(seed);
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    var c = stack.Pop();
                    foreach (var (tile, _) in c.Perimeter())
                    {
                        var n = BuildingAt(tile);
                        if (n == null || n.IsDestroyed) continue;
                        if (IsConductor(n))
                        {
                            if (n.NetworkId == 0) { n.NetworkId = net.Id; net.Members.Add(n); stack.Push(n); }
                        }
                        else if (n.NetworkId == 0)
                        {
                            n.NetworkId = net.Id; net.Members.Add(n);
                        }
                    }
                }
            }
        }

        partial void TickPower()
        {
            if (networksDirty) RebuildNetworks();
            foreach (var net in Networks.Values)
            {
                long supply = 0, demand = 0; int gens = 0, cons = 0;
                net.Batteries.Clear();
                foreach (var b in net.Members)
                {
                    if (b.IsDestroyed) continue;
                    int out_ = b.PowerOutputNow();
                    if (out_ > 0) { supply += out_; gens++; }
                    else if (b is GeneratorBuilding || b is FuelGeneratorBuilding) gens++;
                    if (b.Def.PowerUse > 0) { demand += b.Def.PowerUse; cons++; }
                    if (b is BatteryBuilding bat) net.Batteries.Add(bat);
                }
                net.Supply = supply; net.Demand = demand; net.GeneratorCount = gens; net.ConsumerCount = cons;

                // Batteries soak up the surplus and give it back during a shortfall.
                long fromBatteries = 0;
                if (supply >= demand)
                {
                    long surplus = supply - demand;
                    foreach (var bat in net.Batteries)
                    {
                        if (surplus <= 0) break;
                        long take = System.Math.Min(surplus, bat.RoomToCharge());
                        bat.AddCharge(take); surplus -= take;
                    }
                }
                else
                {
                    long deficit = demand - supply;
                    foreach (var bat in net.Batteries)
                    {
                        if (deficit <= 0) break;
                        long give = System.Math.Min(deficit, bat.AvailableToDischarge());
                        bat.AddCharge(-give); deficit -= give; fromBatteries += give;
                    }
                }
                net.FromBatteries = fromBatteries;
                net.Stored = 0; net.StorageCapacity = 0;
                foreach (var bat in net.Batteries) { net.Stored += bat.Charge; net.StorageCapacity += bat.Capacity; }

                long available = supply + fromBatteries;
                net.Satisfaction = demand == 0 ? 1000 : (int)System.Math.Min(1000L, available * 1000 / demand);
                foreach (var b in net.Members) b.PowerSatisfaction = b.Def.PowerUse > 0 ? net.Satisfaction : 1000;
            }
            foreach (var b in buildings)
                if (b.NetworkId == 0) b.PowerSatisfaction = b.Def.PowerUse > 0 ? 0 : 1000;
        }

        partial void OnBuildingPlaced(Building b) { networksDirty = true; OnLogisticPlaced(b); }
        partial void OnBuildingRemoved(Building b) { networksDirty = true; OnLogisticRemoved(b); }
        partial void OnBuildingDestroyed(Building b) { networksDirty = true; OnLogisticRemoved(b); }

        partial void OnLogisticPlaced(Building b);
        partial void OnLogisticRemoved(Building b);
    }
}
