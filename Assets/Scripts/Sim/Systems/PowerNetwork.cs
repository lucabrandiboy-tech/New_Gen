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
    }
}
