using System.Collections.Generic;
using NewGen.Sim.Entities;

namespace NewGen.Sim.Systems
{
    /// <summary>One connected set of conductors (wires, tubes) and the buildings touching them.</summary>
    public sealed class PowerNetwork
    {
        public int Id;
        public readonly List<Building> Members = new List<Building>();
        public long Supply;        // watts available this tick
        public long Demand;        // watts requested this tick
        public int Satisfaction;   // 0..1000
        public int GeneratorCount, ConsumerCount;
        /// <summary>Batteries on this network, and how much they had to cover this tick.</summary>
        public readonly List<BatteryBuilding> Batteries = new List<BatteryBuilding>();
        public long FromBatteries;
        public long Stored, StorageCapacity;
        public int StoredPermille => StorageCapacity == 0 ? 0 : (int)(Stored * 1000 / StorageCapacity);
    }
}
