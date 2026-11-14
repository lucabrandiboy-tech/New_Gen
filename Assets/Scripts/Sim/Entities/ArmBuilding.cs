using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>
    /// "Telekinesis device": occupies one tile and moves items between two of its four
    /// neighbouring tiles (no diagonals). The player picks which neighbour is the input and
    /// which is the output; the item visibly flies from one to the other. Needs power.
    /// </summary>
    public sealed class ArmBuilding : Building
    {
        /// <summary>Side the item is taken from. Null until configured.</summary>
        public Dir? InputSide;
        /// <summary>Side the item is delivered to. Null until configured.</summary>
        public Dir? OutputSide;

        public string Holding;          // item in flight, null if empty
        public int SwingProgress;       // 0 .. ArmTicksPerSwing*1000; 0 = at the input, full = at the output
        public int ConfigVersion;       // bumps when the sides change (view refresh)

        public bool Configured => InputSide.HasValue && OutputSide.HasValue && InputSide.Value != OutputSide.Value;
        public TilePos InputTile => Origin.Offset(InputSide ?? Facing.Opposite());
        public TilePos OutputTile => Origin.Offset(OutputSide ?? Facing);

        /// <summary>Default wiring when first placed: pull from behind, push in front (so R still works while building fast).</summary>
        public void ApplyDefaultSides()
        {
            InputSide = Facing.Opposite();
            OutputSide = Facing;
            ConfigVersion++;
        }

        public void SetSides(Dir input, Dir output)
        {
            if (input == output) return;
            InputSide = input; OutputSide = output;
            SwingProgress = 0;
            ConfigVersion++;
        }

        public override void Tick(Simulation sim)
        {
            if (!Configured || PowerSatisfaction <= 0) return;
            int step = PowerSatisfaction;                       // 1000 per tick at full power
            int full = Def.ArmTicksPerSwing * 1000;

            if (Holding == null)
            {
                // return to the input side before grabbing again
                if (SwingProgress > 0) { SwingProgress = Fix.Max(0, SwingProgress - step); WorkedThisTick = true; return; }
                var src = sim.BuildingAt(InputTile);
                var dst = sim.BuildingAt(OutputTile);
                if (src == null || src.IsDestroyed || dst == null || dst.IsDestroyed) return;
                string item = TakeFrom(src, dst, sim.Db);
                if (item == null) return;
                Holding = item; WorkedThisTick = true;
            }
            else
            {
                if (SwingProgress < full) { SwingProgress = Fix.Min(full, SwingProgress + step); WorkedThisTick = true; }
                if (SwingProgress < full) return;
                var dst = sim.BuildingAt(OutputTile);
                if (dst == null || dst.IsDestroyed) return;
                if (GiveTo(dst, Holding)) { Holding = null; WorkedThisTick = true; }
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

        /// <summary>0..1000 of the way from the input tile to the output tile.</summary>
        public int FlightPermille => Def.ArmTicksPerSwing == 0 ? 0 : SwingProgress / Def.ArmTicksPerSwing;

        public override void CollectContents(List<ItemStack> into)
        {
            if (Holding != null) into.Add(new ItemStack(Holding, 1));
        }
    }
}
