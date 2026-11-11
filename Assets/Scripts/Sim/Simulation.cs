using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using NewGen.Sim.World;

namespace NewGen.Sim
{
    /// <summary>
    /// The whole game state and its fixed-step update. No Unity types in here.
    /// The view layer calls Tick() 30 times per simulated second and reads state/events.
    /// Everything that changes the state goes through methods on this class (later: commands over the network).
    /// </summary>
    public sealed partial class Simulation
    {
        public readonly ContentDatabase Db;
        public readonly World.World World;
        public readonly Rng Rng;
        public readonly Balance Balance;
        public long Tick { get; private set; }
        public readonly List<SimEvent> Events = new List<SimEvent>();

        readonly Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
        readonly List<Building> buildings = new List<Building>();
        readonly List<Building> buildingsScratch = new List<Building>();
        int nextEntityId = 1;

        public Simulation(ContentDatabase db, ulong seed)
        {
            Db = db; Balance = db.Balance;
            Rng = new Rng(seed);
            var biomes = new AntarcticaTestBiomes(db, seed, db.Balance.TestMapRadiusChunks);
            World = new World.World(db, new WorldGen(db, seed, biomes));
            InitSystems();
        }

        public ulong Seed => World.Gen.Seed;
        public int TicksPerSecond => Balance.TicksPerSecond;
        public long SecondsElapsed => Tick / Balance.TicksPerSecond;

        public IEnumerable<Entity> AllEntities => entities.Values;
        public IReadOnlyList<Building> Buildings => buildings;
        public int EntityCount => entities.Count;

        public Entity GetEntity(int id) => entities.TryGetValue(id, out var e) ? e : null;
        public Building GetBuilding(int id) => GetEntity(id) as Building;

        internal int Register(Entity e)
        {
            e.Id = nextEntityId++;
            entities[e.Id] = e;
            if (e is Building b) buildings.Add(b);
            Events.Add(SimEvent.Entity(SimEventType.EntityAdded, e.Id));
            return e.Id;
        }

        internal void Unregister(Entity e)
        {
            if (e.Removed) return;
            e.Removed = true;
            entities.Remove(e.Id);
            if (e is Building b) buildings.Remove(b);
            Events.Add(SimEvent.Entity(SimEventType.EntityRemoved, e.Id));
        }

        /// <summary>Used when loading a save so ids stay stable.</summary>
        internal void RegisterWithId(Entity e, int id)
        {
            e.Id = id;
            entities[id] = e;
            if (e is Building b) buildings.Add(b);
            if (id >= nextEntityId) nextEntityId = id + 1;
        }
        internal void SetNextEntityId(int id) { if (id > nextEntityId) nextEntityId = id; }
        public int NextEntityId => nextEntityId;

        /// <summary>Advance the world by one fixed step.</summary>
        public void Step()
        {
            Tick++;
            TickPower();
            TickPlayers();
            TickProjectiles();
            buildingsScratch.Clear();
            buildingsScratch.AddRange(buildings);
            foreach (var b in buildingsScratch)
            {
                if (b.Removed || b.IsDestroyed) continue;
                b.WorkedThisTick = false;
                b.Tick(this);
            }
            TickConveyors();
            TickResearch();
            foreach (var b in buildingsScratch)
                if (!b.Removed && b.RefreshState()) Events.Add(SimEvent.Entity(SimEventType.EntityStateChanged, b.Id));
            AfterTick();
        }

        partial void InitSystems();
        partial void TickPower();
        partial void TickPlayers();
        partial void TickProjectiles();
        partial void TickConveyors();
        partial void TickResearch();
        partial void AfterTick();
    }
}
