using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.Sim
{
    public enum PlaceResult { Ok, UnknownBuilding, NotUnlocked, OffMap, BadTerrain, Occupied, PlayerInWay, MissingItems, TooFar }

    public sealed partial class Simulation
    {
        public readonly HashSet<string> UnlockedBuildings = new HashSet<string>();

        /// <summary>Footprint size after rotation.</summary>
        public static void FootprintSize(BuildingDef def, Dir facing, out int w, out int h)
        {
            bool swap = facing == Dir.East || facing == Dir.West;
            w = swap ? def.Height : def.Width;
            h = swap ? def.Width : def.Height;
        }

        public PlaceResult CanPlace(string defId, TilePos origin, Dir facing, Inventory payer = null)
        {
            var def = Db.Building(defId);
            if (def == null) return PlaceResult.UnknownBuilding;
            if (!def.UnlockedAtStart && !UnlockedBuildings.Contains(defId)) return PlaceResult.NotUnlocked;
            if (!def.Rotatable) facing = Dir.North;
            FootprintSize(def, facing, out int w, out int h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new TilePos(origin.X + x, origin.Y + y);
                    if (!World.TerrainAt(p).Buildable) return PlaceResult.BadTerrain;
                    if (World.OccupantAt(p) != 0) return PlaceResult.Occupied;
                    if (IsPlayerOn(p)) return PlaceResult.PlayerInWay;
                }
            if (payer != null && !payer.HasAll(def.Cost)) return PlaceResult.MissingItems;
            return PlaceResult.Ok;
        }

        /// <summary>Place a building. Takes the cost from payer (if given). Returns the building or null.</summary>
        public Building Place(string defId, TilePos origin, Dir facing, Inventory payer = null)
        {
            var result = CanPlace(defId, origin, facing, payer);
            if (result != PlaceResult.Ok) return null;
            var def = Db.Building(defId);
            if (!def.Rotatable) facing = Dir.North;
            if (payer != null) payer.RemoveAll(def.Cost);
            var b = CreateBuilding(def);
            b.Def = def; b.Origin = origin; b.Facing = facing; b.Health = def.MaxHealth;
            b.Pos = Building.CenterOf(origin, b.Width, b.Height);
            Register(b);
            foreach (var t in b.FootprintTiles()) World.SetOccupant(t, b.Id);
            OnBuildingPlaced(b);
            return b;
        }

        /// <summary>Remove a building and hand its contents + cost refund to the receiver (may be null = items lost).</summary>
        public bool Remove(Building b, Inventory receiver)
        {
            if (b == null || b.Removed) return false;
            var refund = new List<ItemStack>();
            if (!b.IsDestroyed) refund.AddRange(b.Def.Cost);
            b.CollectContents(refund);
            if (receiver != null)
                foreach (var s in refund) receiver.Add(s.Item, s.Count);   // overflow is lost (no ground items yet)
            foreach (var t in b.FootprintTiles()) if (World.OccupantAt(t) == b.Id) World.SetOccupant(t, 0);
            OnBuildingRemoved(b);
            Unregister(b);
            return true;
        }

        public Building BuildingAt(TilePos p)
        {
            int id = World.OccupantAt(p);
            return id == 0 ? null : GetBuilding(id);
        }

        /// <summary>Apply damage. Destroyed buildings stay as rubble (state Destroyed) until removed.</summary>
        public void Damage(Building b, int amount)
        {
            if (b == null || b.Removed || b.IsDestroyed) return;
            b.Health = Fix.Max(0, b.Health - amount);
            if (b.Health == 0) OnBuildingDestroyed(b);
            if (b.RefreshState()) Events.Add(SimEvent.Entity(SimEventType.EntityStateChanged, b.Id));
        }

        Building CreateBuilding(BuildingDef def)
        {
            switch (def.Kind)
            {
                case BuildingKind.Chest: return new ChestBuilding { Storage = new Inventory(Db, def.Slots) };
                default:
                    {
                        var custom = CreateSpecialBuilding(def);
                        return custom ?? new Building();
                    }
            }
        }

        // Hooks implemented by the other partial files (power, logistics, machines).
        partial void OnBuildingPlaced(Building b);
        partial void OnBuildingRemoved(Building b);
        partial void OnBuildingDestroyed(Building b);
        Building CreateSpecialBuilding(BuildingDef def) => CreateMachine(def) ?? CreateLogistic(def);
        private partial Building CreateMachine(BuildingDef def);
        private partial Building CreateLogistic(BuildingDef def);
        private partial bool IsPlayerOn(TilePos p);
    }
}
