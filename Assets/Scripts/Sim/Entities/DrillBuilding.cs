using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>Mines the resource (or minable terrain) under its footprint and pushes items out of its front side.</summary>
    public sealed class DrillBuilding : Building
    {
        public int Progress;              // permille-ticks towards the next item
        public string Buffered;           // one item waiting to leave
        public int NextTileIndex;         // round-robin over minable tiles
        readonly List<string> minable = new List<string>();
        bool scanned;

        public override Inventory OutputInventory => null;

        /// <summary>What this drill can mine, one entry per footprint tile that has something.</summary>
        public void Rescan(Simulation sim)
        {
            minable.Clear();
            foreach (var t in FootprintTiles())
            {
                var tile = sim.World.GetTile(t);
                if (tile.Resource != 0) { minable.Add(sim.Db.Resource(tile.Resource).YieldsItem); continue; }
                var terr = sim.Db.Terrain(tile.Terrain);
                if (terr.YieldsItem != null) minable.Add(terr.YieldsItem);
            }
            scanned = true;
        }

        public bool HasAnythingToMine(Simulation sim) { if (!scanned) Rescan(sim); return minable.Count > 0; }
        public IReadOnlyList<string> Minable => minable;

        public override void Tick(Simulation sim)
        {
            if (!scanned) Rescan(sim);
            // try to push the buffered item out first
            if (Buffered != null)
            {
                if (sim.PushItemTo(FrontTile(), Buffered)) Buffered = null;
                else return;   // output blocked: stall
            }
            if (minable.Count == 0 || PowerSatisfaction <= 0) return;
            Progress += PowerSatisfaction;                              // 1000 per tick at full power
            WorkedThisTick = true;
            int needed = Def.MineTicksPerItem * 1000;
            if (Progress < needed) return;
            Progress -= needed;
            Buffered = minable[NextTileIndex % minable.Count];
            NextTileIndex = (NextTileIndex + 1) % minable.Count;
            if (sim.PushItemTo(FrontTile(), Buffered)) Buffered = null;
        }

        public override void CollectContents(List<ItemStack> into)
        {
            if (Buffered != null) into.Add(new ItemStack(Buffered, 1));
        }
    }
}
