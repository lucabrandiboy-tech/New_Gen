using System;
using System.Collections.Generic;
using NewGen.Sim.Content;

namespace NewGen.Sim.Entities
{
    /// <summary>Fixed number of slots, each holding one item type up to its stack size.</summary>
    public sealed class Inventory
    {
        public readonly ItemStack[] Slots;   // null = empty slot
        readonly ContentDatabase db;
        /// <summary>Optional per-slot item filter (null = any).</summary>
        public readonly string[] Filters;
        public int Version;                  // bumps on every change (UI refresh)

        public Inventory(ContentDatabase db, int slots)
        {
            this.db = db;
            Slots = new ItemStack[slots];
            Filters = new string[slots];
        }

        public int SlotCount => Slots.Length;
        public bool IsEmpty { get { foreach (var s in Slots) if (s != null && s.Count > 0) return false; return true; } }

        public int Count(string item)
        {
            int n = 0;
            foreach (var s in Slots) if (s != null && s.Item == item) n += s.Count;
            return n;
        }

        public bool Has(string item, int count) => Count(item) >= count;

        public bool HasAll(List<ItemStack> stacks)
        {
            foreach (var s in stacks) if (!Has(s.Item, s.Count)) return false;
            return true;
        }

        /// <summary>How many of this item could still fit.</summary>
        public int SpaceFor(string item)
        {
            int stack = db.StackSize(item), space = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Filters[i] != null && Filters[i] != item) continue;
                var s = Slots[i];
                if (s == null) space += stack;
                else if (s.Item == item) space += stack - s.Count;
            }
            return space;
        }

        public bool CanAdd(string item, int count) => SpaceFor(item) >= count;

        /// <summary>Adds as many as fit. Returns how many did NOT fit.</summary>
        public int Add(string item, int count)
        {
            if (count <= 0) return 0;
            int stack = db.StackSize(item);
            // top up existing stacks first
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                var s = Slots[i];
                if (s == null || s.Item != item || s.Count >= stack) continue;
                int take = Math.Min(count, stack - s.Count);
                s.Count += take; count -= take; Version++;
            }
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                if (Slots[i] != null) continue;
                if (Filters[i] != null && Filters[i] != item) continue;
                int take = Math.Min(count, stack);
                Slots[i] = new ItemStack(item, take); count -= take; Version++;
            }
            return count;
        }

        /// <summary>Removes up to count. Returns how many were actually removed.</summary>
        public int Remove(string item, int count)
        {
            int removed = 0;
            for (int i = Slots.Length - 1; i >= 0 && removed < count; i--)
            {
                var s = Slots[i];
                if (s == null || s.Item != item) continue;
                int take = Math.Min(count - removed, s.Count);
                s.Count -= take; removed += take; Version++;
                if (s.Count == 0) Slots[i] = null;
            }
            return removed;
        }

        public bool RemoveAll(List<ItemStack> stacks)
        {
            if (!HasAll(stacks)) return false;
            foreach (var s in stacks) Remove(s.Item, s.Count);
            return true;
        }

        /// <summary>Take one item of any type (for arms pulling from chests). Returns null if empty.</summary>
        public string TakeAny(Func<string, bool> accept = null)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                var s = Slots[i];
                if (s == null || s.Count <= 0) continue;
                if (accept != null && !accept(s.Item)) continue;
                string item = s.Item;
                s.Count--; Version++;
                if (s.Count == 0) Slots[i] = null;
                return item;
            }
            return null;
        }

        public void Clear() { for (int i = 0; i < Slots.Length; i++) Slots[i] = null; Version++; }

        /// <summary>Move everything possible into another inventory. Returns true if this one is now empty.</summary>
        public bool TransferAllTo(Inventory other)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                var s = Slots[i];
                if (s == null) continue;
                int left = other.Add(s.Item, s.Count);
                s.Count = left; Version++;
                if (left == 0) Slots[i] = null;
            }
            return IsEmpty;
        }

        public List<ItemStack> ToStacks()
        {
            var list = new List<ItemStack>();
            foreach (var s in Slots) if (s != null && s.Count > 0) list.Add(new ItemStack(s.Item, s.Count));
            return list;
        }
    }
}
