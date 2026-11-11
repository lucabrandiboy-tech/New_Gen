using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    public struct ConveyorItem
    {
        public string Item;
        public int Pos;      // 0 (entry) .. 255 (exit) along the flow direction
    }

    /// <summary>
    /// One tile of tube (items) or data-wire (data cells). Facing = flow direction.
    /// Items are kept sorted by Pos, tail first. Tubes stop when unpowered; wires always run.
    /// </summary>
    public sealed class ConveyorBuilding : Building
    {
        public readonly List<ConveyorItem> Items = new List<ConveyorItem>();
        public int ItemsVersion;
        /// <summary>Which neighbour feeds this tile (for corner sprites): the direction FROM this tile TO the feeder, or null.</summary>
        public Dir? FeedFrom;

        public bool IsWire => Def.Kind == BuildingKind.Wire;
        public int Spacing => Fix.SubPerTile / Fix.Max(1, Def.ItemsPerTile);
        public const int Exit = Fix.SubPerTile - 1;

        public bool CarriesItem(ContentDatabase db, string item)
        {
            var def = db.Item(item);
            bool isCell = def != null && def.IsDataCell;
            return IsWire ? isCell : !isCell;
        }

        public int SpeedThisTick()
        {
            if (IsDestroyed) return 0;
            if (IsWire) return Def.SubPerTick;
            if (PowerSatisfaction <= 0) return 0;
            return Fix.Max(1, Fix.Permille(Def.SubPerTick, PowerSatisfaction));
        }

        /// <summary>Room at the entry for one more item?</summary>
        public bool HasRoomAt(int pos)
        {
            foreach (var it in Items) if (Fix.Abs(it.Pos - pos) < Spacing) return false;
            return true;
        }

        public bool HasRoomAtEntry => Items.Count == 0 || Items[0].Pos >= Spacing;

        public override bool Accepts(string item) => !IsDestroyed && HasRoomAtEntry;

        public bool TryInsert(string item, int pos = 0)
        {
            if (IsDestroyed || !HasRoomAt(pos)) return false;
            int i = 0;
            while (i < Items.Count && Items[i].Pos < pos) i++;
            Items.Insert(i, new ConveyorItem { Item = item, Pos = pos });
            ItemsVersion++;
            return true;
        }

        /// <summary>Remove the item closest to the exit (for arms). Null if empty.</summary>
        public string TakeHead()
        {
            if (Items.Count == 0) return null;
            string item = Items[Items.Count - 1].Item;
            Items.RemoveAt(Items.Count - 1);
            ItemsVersion++;
            return item;
        }

        public string PeekHead() => Items.Count == 0 ? null : Items[Items.Count - 1].Item;

        /// <summary>Pass 1: move items forward inside this tile, respecting spacing. The head stops at Exit.</summary>
        public override void Tick(Simulation sim)
        {
            int speed = SpeedThisTick();
            if (speed == 0 || Items.Count == 0) return;
            int limit = Exit;
            bool moved = false;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                var it = Items[i];
                int target = Fix.Min(it.Pos + speed, limit);
                if (target != it.Pos) { it.Pos = target; Items[i] = it; moved = true; }
                limit = it.Pos - Spacing;
            }
            if (moved) { ItemsVersion++; WorkedThisTick = true; }
        }

        /// <summary>Pass 2: hand the head item to the next conveyor if it is at the exit and the next tile has room.</summary>
        public void TryTransfer(Simulation sim)
        {
            if (Items.Count == 0 || IsDestroyed) return;
            var head = Items[Items.Count - 1];
            if (head.Pos < Exit) return;
            if (SpeedThisTick() == 0) return;
            var next = sim.BuildingAt(FrontTile()) as ConveyorBuilding;
            if (next == null || next.IsWire != IsWire || next.Facing == Facing.Opposite()) return;
            if (!next.TryInsert(head.Item, 0)) return;
            Items.RemoveAt(Items.Count - 1);
            ItemsVersion++;
            if (next.FeedFrom != Facing.Opposite()) next.FeedFrom = Facing.Opposite();
        }

        public override void CollectContents(List<ItemStack> into)
        {
            foreach (var it in Items) into.Add(new ItemStack(it.Item, 1));
        }
    }
}
