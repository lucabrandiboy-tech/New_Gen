using System.Collections.Generic;
using NewGen.Sim.Core;

namespace NewGen.Sim.Content
{
    /// <summary>What a building fundamentally does. Each kind maps to one simulation component.</summary>
    public enum BuildingKind
    {
        Static,      // decoration / no behaviour
        Generator,   // produces power (pod reactor)
        Drill,       // mines the resource under its footprint
        Crafter,     // runs recipes (smelter, assembler, encoder)
        DataCenter,  // consumes data cells for research
        Chest,       // storage
        Tube,        // item conveyor
        Wire,        // data-cell conveyor that also carries power
        Arm,         // moves items between neighbours
        Heater,      // warms the player / clears snow (later)
    }

    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public int StackSize = 100;
        public string Category = "material";   // material, cell, tool, ...
        public bool IsDataCell => Category == "cell";
        public string SpriteKey => IsDataCell ? "cell_" + Id.Replace("cell_", "") : "item_" + Id;
    }

    public sealed class ItemStack
    {
        public string Item;
        public int Count;
        public ItemStack(string item, int count) { Item = item; Count = count; }
        public override string ToString() => Count + "x " + Item;
    }

    public sealed class RecipeDef
    {
        public string Id;
        public string Name;
        public List<ItemStack> Inputs = new List<ItemStack>();
        public List<ItemStack> Outputs = new List<ItemStack>();
        public int Ticks;                     // crafting time in sim ticks
        public string Crafter = "hand";       // hand, smelter, assembler, encoder (matches BuildingDef.CrafterType)
        public bool UnlockedAtStart = true;
    }

    public sealed class BuildingDef
    {
        public string Id;
        public string Name;
        public int Width = 1, Height = 1;
        public BuildingKind Kind = BuildingKind.Static;
        public bool Rotatable = true;
        public int MaxHealth = 100;
        public List<ItemStack> Cost = new List<ItemStack>();   // items consumed to build (empty = free, until recipes exist)
        public bool UnlockedAtStart = true;

        // Power (watts). Consumers list PowerUse; generators list PowerOutput.
        public int PowerUse;
        public int PowerOutput;
        /// <summary>Generator only: ticks until output has decayed to DecayFloorPermille of the start value (0 = never).</summary>
        public int DecayTicks;
        public int DecayFloorPermille = 1000;

        // Drill
        public int MineTicksPerItem;          // ticks per mined item at full power

        // Crafter
        public string CrafterType;            // smelter, assembler, encoder
        public int InputSlots = 2, OutputSlots = 1;

        // Chest
        public int Slots;

        // Conveyors (tube / wire)
        public int SubPerTick;                // item travel speed in sub-units per tick (256 = one tile per tick)
        public int ItemsPerTile = 4;

        // Arm
        public int ArmTicksPerSwing = 30;

        // DataCenter
        public int ResearchPerTick;           // research points per tick when powered + cooled
        public int HeatPerTick;               // heat generated per tick (cooled by cold terrain)
        public int MaxHeat = 1000;

        // Heat given to the player standing nearby (temperature per tick within radius)
        public int WarmthRadiusTiles;
        public int WarmthPerTick;

        public bool IsConveyor => Kind == BuildingKind.Tube || Kind == BuildingKind.Wire;
        public string SpriteKey(string state) => "bld_" + Id + "_" + state;
    }

    public sealed class TechDef
    {
        public string Id;
        public string Name;
        public string Branch = "logistics";   // military, logistics, compute, bio
        public int Tier = 1;
        public Dictionary<string, int> CellCost = new Dictionary<string, int>(); // cell item id -> count
        public int ResearchPoints = 100;      // total points a data center must produce (cells are consumed as points accrue)
        public List<string> Prerequisites = new List<string>();
        public List<string> UnlocksRecipes = new List<string>();
        public List<string> UnlocksBuildings = new List<string>();
        public bool IsFinal;                  // the time-travel reveal
    }

    public sealed class TerrainDef
    {
        public string Id;                     // snow, ice, rock (matches tile_<id>_NN sprites)
        public string Name;
        public byte Index;                    // compact id stored per tile
        public bool Buildable = true;
        public bool Walkable = true;
        public int WalkSpeedPermille = 1000;
        public int ColdPerTick;               // temperature drained per tick while standing on it
        public int CoolingPermille = 1000;    // data-center cooling multiplier
    }

    public sealed class ResourceDef
    {
        public string Id;                     // iron, copper, coal, stone, ice
        public string Name;
        public byte Index;                    // 0 = none
        public string YieldsItem;             // item id produced by mining
        public int HandMineTicks = 60;        // ticks per item when mined by hand
        public int SpawnWeight = 100;         // relative frequency in world gen
        public int PatchRadiusMin = 4, PatchRadiusMax = 9;
        public string SpriteKey => "res_" + Id + "_01";
    }

    /// <summary>Tunable numbers. Lives in balance.json so the owner can tweak without code.</summary>
    public sealed class Balance
    {
        public int TicksPerSecond = 30;
        public int PlayerWalkSubPerTick = 68;          // ~8 tiles/s at 30 tps
        public int PlayerMaxHealth = 100;
        public int PlayerMaxTemperature = 1000;         // 1000 = comfortable, 0 = dead
        public int PlayerBaseColdPerTick = 1;           // drain everywhere outdoors
        public int PlayerInventorySlots = 40;
        public int PlayerReach = 6;                     // tiles
        public int PlayerHandMineSpeedPermille = 1000;
        public int PlayerShootTicks = 10;               // ticks between shots
        public int ProjectileSpeedSubPerTick = 400;
        public int ProjectileDamage = 10;
        public int ProjectileRangeTiles = 12;
        public int ManualSaveCooldownSeconds = 600;     // 10 minutes
        public int IronmanSaveCooldownSeconds = 3600;   // 1 hour
        public int ResearchQueueMax = 10;
        public int ChunkLoadRadius = 3;                 // chunks kept simulated around the player
        public int MapSeedDefault = 2026;
        public int TestMapRadiusChunks = 16;            // Milestone-1 test map: chunks from origin before the "edge of the world"
    }
}
