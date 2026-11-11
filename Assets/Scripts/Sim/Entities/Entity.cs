using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    public enum EntityKind : byte { Building, Player, Projectile }

    /// <summary>Anything in the world with an id and a position.</summary>
    public abstract class Entity
    {
        public int Id;
        public abstract EntityKind Kind { get; }
        /// <summary>World position in sub-units. For buildings: centre of the footprint.</summary>
        public SubPos Pos;
        public bool Removed;

        public TilePos Tile => Pos.Tile;
    }
}
