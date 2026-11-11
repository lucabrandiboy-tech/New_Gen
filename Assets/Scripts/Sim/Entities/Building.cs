using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>Visual + logical state. The view picks the sprite from this.</summary>
    public enum BuildingState : byte { Idle, Working, NoPower, Damaged, Destroyed }

    /// <summary>A placed structure. Subclasses add behaviour per BuildingKind.</summary>
    public class Building : Entity
    {
        public override EntityKind Kind => EntityKind.Building;
        public BuildingDef Def;
        /// <summary>Bottom-left tile of the (rotated) footprint.</summary>
        public TilePos Origin;
        public Dir Facing = Dir.North;
        public int Health;
        public BuildingState State = BuildingState.Idle;
        /// <summary>Power network this building is attached to (0 = none).</summary>
        public int NetworkId;
        /// <summary>0..1000: how much of the requested power it received last tick.</summary>
        public int PowerSatisfaction = 1000;
        /// <summary>Set by subclasses: did it do useful work this tick (for Working state + glow)?</summary>
        public bool WorkedThisTick;
        public int StateVersion;   // bumps when State changes (view refresh)

        public int Width => Facing == Dir.East || Facing == Dir.West ? Def.Height : Def.Width;
        public int Height => Facing == Dir.East || Facing == Dir.West ? Def.Width : Def.Height;
        public bool IsDestroyed => State == BuildingState.Destroyed;
        public bool NeedsPower => Def.PowerUse > 0;
        public bool HasPower => !NeedsPower || PowerSatisfaction > 0;

        public bool Covers(TilePos p) => p.X >= Origin.X && p.Y >= Origin.Y && p.X < Origin.X + Width && p.Y < Origin.Y + Height;

        public IEnumerable<TilePos> FootprintTiles()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    yield return new TilePos(Origin.X + x, Origin.Y + y);
        }

        /// <summary>Tiles just outside the footprint, each with the direction from the building to that tile.</summary>
        public IEnumerable<(TilePos tile, Dir side)> Perimeter()
        {
            for (int x = 0; x < Width; x++) { yield return (new TilePos(Origin.X + x, Origin.Y + Height), Dir.North); yield return (new TilePos(Origin.X + x, Origin.Y - 1), Dir.South); }
            for (int y = 0; y < Height; y++) { yield return (new TilePos(Origin.X + Width, Origin.Y + y), Dir.East); yield return (new TilePos(Origin.X - 1, Origin.Y + y), Dir.West); }
        }

        /// <summary>The tile in front of the building on its facing side, centred (drill output, arm target).</summary>
        public TilePos FrontTile()
        {
            switch (Facing)
            {
                case Dir.North: return new TilePos(Origin.X + Width / 2, Origin.Y + Height);
                case Dir.South: return new TilePos(Origin.X + Width / 2, Origin.Y - 1);
                case Dir.East: return new TilePos(Origin.X + Width, Origin.Y + Height / 2);
                default: return new TilePos(Origin.X - 1, Origin.Y + Height / 2);
            }
        }

        public TilePos BackTile()
        {
            switch (Facing)
            {
                case Dir.North: return new TilePos(Origin.X + Width / 2, Origin.Y - 1);
                case Dir.South: return new TilePos(Origin.X + Width / 2, Origin.Y + Height);
                case Dir.East: return new TilePos(Origin.X - 1, Origin.Y + Height / 2);
                default: return new TilePos(Origin.X + Width, Origin.Y + Height / 2);
            }
        }

        public static SubPos CenterOf(TilePos origin, int w, int h) => new SubPos(Fix.SubOf(origin.X) + w * Fix.SubPerTile / 2, Fix.SubOf(origin.Y) + h * Fix.SubPerTile / 2);

        /// <summary>Recompute State from health/power/work. Returns true if it changed.</summary>
        public bool RefreshState()
        {
            BuildingState s;
            if (Health <= 0) s = BuildingState.Destroyed;
            else if (NeedsPower && PowerSatisfaction == 0) s = BuildingState.NoPower;
            else if (Health * 2 < Def.MaxHealth) s = BuildingState.Damaged;
            else if (WorkedThisTick) s = BuildingState.Working;
            else s = BuildingState.Idle;
            if (s == State) return false;
            State = s; StateVersion++;
            return true;
        }

        /// <summary>Inventories an arm may take from / put into. Default: none.</summary>
        public virtual Inventory OutputInventory => null;
        public virtual Inventory InputInventory => null;
        /// <summary>Can this building accept the item right now (for arms / tubes)?</summary>
        public virtual bool Accepts(string item) => InputInventory != null && InputInventory.CanAdd(item, 1);

        /// <summary>Per-tick behaviour. Subclasses override. Called only for loaded, non-destroyed buildings.</summary>
        public virtual void Tick(Simulation sim) { }

        /// <summary>Everything inside, for refunds when deconstructed.</summary>
        public virtual void CollectContents(List<ItemStack> into)
        {
            if (InputInventory != null) into.AddRange(InputInventory.ToStacks());
            if (OutputInventory != null && OutputInventory != InputInventory) into.AddRange(OutputInventory.ToStacks());
        }
    }

    public sealed class ChestBuilding : Building
    {
        public Inventory Storage;
        public override Inventory OutputInventory => Storage;
        public override Inventory InputInventory => Storage;
    }
}
