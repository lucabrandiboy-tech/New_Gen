using System.Collections.Generic;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>What the player wants to do this tick. Set by the input layer; later sent over the network.</summary>
    public struct PlayerInput
    {
        public int MoveX, MoveY;          // -1000..1000
        public SubPos Aim;                // world position of the cursor
        public bool Fire;
        public bool Mine;                 // hold to mine the tile under the cursor
    }

    public sealed class CraftJob
    {
        public string RecipeId;
        public int Progress;              // permille-ticks
        public bool Started;              // inputs consumed
    }

    public sealed class Player : Entity
    {
        public override EntityKind Kind => EntityKind.Player;
        public int Health, Temperature;
        public Inventory Inventory;
        public PlayerInput Input;
        public Dir Facing = Dir.South;
        public bool Moving;
        public bool Dead;
        public int ShootCooldown;
        public int MineProgress;
        public TilePos MineTarget;
        public bool Mining;
        public readonly List<CraftJob> CraftQueue = new List<CraftJob>();
        public int QuickbarPage;
        public int Version;               // bumps on inventory/health/temperature changes worth a UI refresh

        public const int Radius = 80;     // collision half-size in sub-units (~0.3 tile)
    }

    public sealed class Projectile : Entity
    {
        public override EntityKind Kind => EntityKind.Projectile;
        public int VelX, VelY;            // sub-units per tick
        public int RangeLeft;             // sub-units
        public int Damage;
        public int OwnerId;
    }
}
