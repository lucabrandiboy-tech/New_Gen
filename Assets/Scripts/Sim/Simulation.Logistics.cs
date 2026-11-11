using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.Sim
{
    public sealed partial class Simulation
    {
        readonly List<ConveyorBuilding> conveyors = new List<ConveyorBuilding>();
        readonly List<ConveyorBuilding> conveyorsScratch = new List<ConveyorBuilding>();

        public IReadOnlyList<ConveyorBuilding> Conveyors => conveyors;

        private partial Building CreateLogistic(BuildingDef def)
        {
            switch (def.Kind)
            {
                case BuildingKind.Tube:
                case BuildingKind.Wire: return new ConveyorBuilding();
                case BuildingKind.Arm: return new ArmBuilding();
                default: return null;
            }
        }

        partial void OnLogisticPlaced(Building b)
        {
            if (b is ConveyorBuilding c) { conveyors.Add(c); RefreshFeeds(c); }
        }

        partial void OnLogisticRemoved(Building b)
        {
            if (b is ConveyorBuilding c)
            {
                conveyors.Remove(c);
                // neighbours that were fed by this tile forget it
                var next = BuildingAt(c.FrontTile()) as ConveyorBuilding;
                if (next != null && next.FeedFrom == c.Facing.Opposite()) RefreshFeeds(next);
            }
        }

        /// <summary>Work out which neighbour feeds a conveyor (prefers the one behind it). Used for corner sprites.</summary>
        void RefreshFeeds(ConveyorBuilding c)
        {
            c.FeedFrom = null;
            var back = BuildingAt(c.BackTile()) as ConveyorBuilding;
            if (back != null && back.IsWire == c.IsWire && back.Facing == c.Facing) { c.FeedFrom = c.Facing.Opposite(); }
            else
            {
                foreach (var (tile, side) in c.Perimeter())
                {
                    var n = BuildingAt(tile) as ConveyorBuilding;
                    if (n != null && n.IsWire == c.IsWire && n.FrontTile() == c.Origin && n.Facing != c.Facing.Opposite()) { c.FeedFrom = side; break; }
                }
            }
            // this tile may itself now feed the one in front
            var front = BuildingAt(c.FrontTile()) as ConveyorBuilding;
            if (front != null && front.IsWire == c.IsWire && front.Facing != c.Facing.Opposite() && front.FeedFrom == null)
                front.FeedFrom = c.Facing.Opposite();
        }

        partial void TickConveyors()
        {
            conveyorsScratch.Clear();
            conveyorsScratch.AddRange(conveyors);
            foreach (var c in conveyorsScratch) if (!c.Removed) c.TryTransfer(this);
        }

        /// <summary>Push one item into whatever is on a tile (conveyor entry or building input). Used by drills/crafters.</summary>
        public bool PushItemTo(TilePos tile, string item)
        {
            var b = BuildingAt(tile);
            if (b == null || b.IsDestroyed) return false;
            if (b is ConveyorBuilding c) return c.CarriesItem(Db, item) && c.TryInsert(item, 0);
            var inv = b.InputInventory;
            if (inv == null || !b.Accepts(item)) return false;
            return inv.Add(item, 1) == 0;
        }
    }
}
