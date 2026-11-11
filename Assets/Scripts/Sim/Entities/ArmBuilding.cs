using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>Picks one item from the tile behind it and drops it on the tile in front. Needs power.</summary>
    public sealed class ArmBuilding : Building
    {
        public string Holding;          // item in hand, null if empty
        public int SwingProgress;       // 0..ArmTicksPerSwing
        public bool SwingingForward;    // true = carrying to the front

        public override void Tick(Simulation sim)
        {
            if (PowerSatisfaction <= 0) return;
            int step = Fix.Max(1, Fix.Permille(1000, PowerSatisfaction));   // permille of a tick's progress
            // progress in permille-ticks: ArmTicksPerSwing ticks at full power
            if (Holding == null)
            {
                if (SwingProgress > 0) { SwingProgress = Fix.Max(0, SwingProgress - step); WorkedThisTick = true; return; } // returning
                var src = sim.BuildingAt(BackTile());
                if (src == null || src.IsDestroyed) return;
                var dst = sim.BuildingAt(FrontTile());
                if (dst == null || dst.IsDestroyed) return;
                string item = TakeFrom(src, dst, sim.Db);
                if (item == null) return;
                Holding = item; SwingingForward = true; SwingProgress = 0; WorkedThisTick = true;
            }
            else
            {
                int full = Def.ArmTicksPerSwing * 1000;
                if (SwingProgress < full) { SwingProgress = Fix.Min(full, SwingProgress + step); WorkedThisTick = true; }
                if (SwingProgress < full) return;
                var dst = sim.BuildingAt(FrontTile());
                if (dst == null || dst.IsDestroyed) return;
                if (GiveTo(dst, Holding))
                {
                    Holding = null; SwingingForward = false; SwingProgress = full; WorkedThisTick = true;
                }
            }
        }

        static string TakeFrom(Building src, Building dst, ContentDatabase db)
        {
            if (src is ConveyorBuilding conv)
            {
                string head = conv.PeekHead();
                if (head == null || !CanDeliver(dst, head, db)) return null;
                return conv.TakeHead();
            }
            var inv = src.OutputInventory;
            if (inv == null) return null;
            return inv.TakeAny(item => CanDeliver(dst, item, db));
        }

        static bool CanDeliver(Building dst, string item, ContentDatabase db)
        {
            if (dst is ConveyorBuilding c) return c.CarriesItem(db, item) && c.HasRoomAtEntry;
            return dst.Accepts(item);
        }

        static bool GiveTo(Building dst, string item)
        {
            if (dst is ConveyorBuilding c) return c.TryInsert(item, 0);
            var inv = dst.InputInventory;
            if (inv == null || !dst.Accepts(item)) return false;
            return inv.Add(item, 1) == 0;
        }

        public override void CollectContents(List<ItemStack> into)
        {
            if (Holding != null) into.Add(new ItemStack(Holding, 1));
        }
    }
}
