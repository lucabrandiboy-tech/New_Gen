using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>
    /// Turns data cells into research points for the tech at the head of the queue.
    /// Generates heat while working; the ground under it cools it (cold terrain = bonus). Overheats at MaxHeat.
    /// </summary>
    public sealed class DataCenterBuilding : Building
    {
        public Inventory Cells;
        public int Heat;                 // 0..MaxHeat (in heat units * 1000)
        public bool Overheated;
        public bool MissingCells;
        public int CoolingPerTickPermille;   // computed from the terrain under the footprint

        public override Inventory InputInventory => Cells;

        public override bool Accepts(string item)
        {
            var def = Def == null ? null : CellsDb?.Item(item);
            return def != null && def.IsDataCell && Cells.CanAdd(item, 1);
        }
        public ContentDatabase CellsDb;

        public void ComputeCooling(Simulation sim)
        {
            long sum = 0; int n = 0;
            foreach (var t in FootprintTiles()) { sum += sim.World.TerrainAt(t).CoolingPermille; n++; }
            int avg = n == 0 ? 1000 : (int)(sum / n);
            CoolingPerTickPermille = Def.HeatPerTick * avg;   // heat units per tick * 1000
        }

        public override void Tick(Simulation sim)
        {
            if (CoolingPerTickPermille == 0) ComputeCooling(sim);
            // cool down every tick
            Heat = Fix.Max(0, Heat - CoolingPerTickPermille);
            if (Overheated && Heat <= Def.MaxHeat * 1000 / 2) Overheated = false;

            MissingCells = false;
            if (Overheated || PowerSatisfaction <= 0) return;
            var research = sim.Research;
            if (research.Current == null) return;
            if (!research.TryContribute(this, sim)) { MissingCells = true; return; }
            WorkedThisTick = true;
            Heat += Def.HeatPerTick * 1000 * PowerSatisfaction / 1000;
            if (Heat >= Def.MaxHeat * 1000) { Overheated = true; sim.Events.Add(SimEvent.Alert("alert.overheat", Origin, Id)); }
        }

        public int HeatPermille => Def.MaxHeat == 0 ? 0 : Heat / Def.MaxHeat;
    }
}
