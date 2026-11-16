using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>
    /// One tile that both splits and merges belt lines. Anything can push an item in from any side;
    /// the router then sends each item out to whichever neighbours will take it, taking turns so a
    /// line feeding two machines keeps both fed. It never pushes an item back the way it came, and
    /// never into a belt that is pointing at it.
    /// </summary>
    public sealed class RouterBuilding : Building
    {
        public struct Held
        {
            public string Item;
            public int Ticks;      // counts down to 0, then the item may leave
            public Dir From;
        }

        public readonly List<Held> Items = new List<Held>();
        public int ItemsVersion;
        int nextOut;               // round-robin cursor over the four sides

        public bool HasRoom => Items.Count < Fix.Max(1, Def.RouterSlots);
        public override bool Accepts(string item) => !IsDestroyed && HasRoom;

        public override bool TryTakeIn(string item, Dir from)
        {
            if (!Accepts(item)) return false;
            Items.Add(new Held { Item = item, Ticks = Fix.Max(1, Def.RouterTicksPerItem), From = from });
            ItemsVersion++;
            return true;
        }

        public override void Tick(Simulation sim)
        {
            if (PowerSatisfaction <= 0 || Items.Count == 0) return;
            for (int i = 0; i < Items.Count; i++)
            {
                var h = Items[i];
                if (h.Ticks > 0) { h.Ticks--; Items[i] = h; }
            }
            WorkedThisTick = true;

            var head = Items[0];
            if (head.Ticks > 0) return;

            for (int attempt = 0; attempt < 4; attempt++)
            {
                var side = (Dir)((nextOut + attempt) & 3);
                if (side == head.From) continue;                       // never straight back out
                var target = Origin.Offset(side);
                var nb = sim.BuildingAt(target);
                if (nb == null || nb.IsDestroyed) continue;
                if (nb is ConveyorBuilding cv && cv.FrontTile() == Origin) continue;   // that belt feeds us
                if (nb is ConveyorBuilding cv2 && !cv2.CarriesItem(sim.Db, head.Item)) continue;
                if (!nb.TryTakeIn(head.Item, side.Opposite())) continue;
                Items.RemoveAt(0);
                ItemsVersion++;
                nextOut = ((int)side + 1) & 3;
                return;
            }
        }

        public override void CollectContents(List<ItemStack> into)
        {
            foreach (var h in Items) into.Add(new ItemStack(h.Item, 1));
        }
    }
}
