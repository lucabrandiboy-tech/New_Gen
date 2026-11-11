using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    public static class EntityTests
    {
        public static Simulation NewSim(ulong seed = 2026) => new Simulation(ContentTests.Db, seed);

        public static void Run(string repoRoot)
        {
            Check.Section("Inventory");
            var db = ContentTests.Db;
            var inv = new Inventory(db, 2);
            Check.Eq(0, inv.Add("iron_ore", 60), "60 ore fits in 2 slots of 50");
            Check.Eq(60, inv.Count("iron_ore"), "count");
            Check.Eq(10, inv.Add("iron_ore", 50), "overflow reported");
            Check.Eq(100, inv.Count("iron_ore"), "full");
            Check.Eq(30, inv.Remove("iron_ore", 30), "remove");
            Check.Eq(70, inv.Count("iron_ore"), "after remove");
            Check.True(!inv.CanAdd("coal", 1), "no room for another type while both slots used");
            inv.Remove("iron_ore", 70);
            Check.True(inv.IsEmpty, "empty after removing all");
            inv.Filters[0] = "coal";
            Check.Eq(0, inv.Add("iron_ore", 50), "ore goes to the unfiltered slot");
            Check.Eq(50, inv.Add("iron_ore", 50), "filtered slot refuses ore");
            Check.Eq("iron_ore", inv.TakeAny(), "take any");
            Check.Eq(49, inv.Count("iron_ore"), "take any removed one");

            Check.Section("Placement");
            var sim = NewSim();
            Check.Eq(PlaceResult.Ok, sim.CanPlace("chest", new TilePos(2, 2), Dir.North), "chest on snow ok");
            var chest = sim.Place("chest", new TilePos(2, 2), Dir.North);
            Check.True(chest != null && chest is ChestBuilding, "chest placed");
            Check.Eq(PlaceResult.Occupied, sim.CanPlace("chest", new TilePos(2, 2), Dir.North), "occupied");
            Check.Eq(chest, sim.BuildingAt(new TilePos(2, 2)), "lookup by tile");
            var drill = sim.Place("drill", new TilePos(1, 1), Dir.East);
            Check.True(drill == null, "drill overlaps chest -> not placed");
            drill = sim.Place("drill", new TilePos(4, 4), Dir.East);
            Check.True(drill != null, "drill placed next to chest");
            Check.Eq(3, drill.Width, "3x3 footprint");
            Check.True(drill.Covers(new TilePos(6, 6)) && !drill.Covers(new TilePos(7, 6)), "covers footprint only");
            Check.Eq(new TilePos(7, 5), drill.FrontTile(), "front tile east of a 3x3 at (4,4)");
            Check.Eq(new TilePos(3, 5), drill.BackTile(), "back tile");
            int edge = db.Balance.TestMapRadiusChunks * 32 + 80;
            Check.Eq(PlaceResult.BadTerrain, sim.CanPlace("chest", new TilePos(edge, 0), Dir.North), "cannot build on ocean");

            var pay = new Inventory(db, 4);
            Check.Eq(PlaceResult.Ok, sim.CanPlace("chest", new TilePos(10, 10), Dir.North, pay), "free (no cost yet) with empty payer");

            Check.Section("Removal / damage");
            ((ChestBuilding)chest).Storage.Add("coal", 7);
            var bag = new Inventory(db, 10);
            Check.True(sim.Remove(chest, bag), "remove chest");
            Check.Eq(7, bag.Count("coal"), "contents refunded");
            Check.Eq(0, sim.World.OccupantAt(new TilePos(2, 2)), "tile freed");
            Check.True(sim.GetEntity(chest.Id) == null, "entity gone");
            sim.Damage(drill, 200);
            Check.Eq(BuildingState.Damaged, drill.State, "damaged below half");
            sim.Damage(drill, 999);
            Check.Eq(BuildingState.Destroyed, drill.State, "destroyed at 0");
            Check.Eq(drill, sim.BuildingAt(new TilePos(5, 5)), "rubble still occupies tiles");
            int events = sim.Events.Count;
            Check.True(events >= 4, "events emitted: " + events);
        }
    }
}
