using NewGen.Sim.Core;

namespace NewGen.Sim.World
{
    /// <summary>32x32 tiles. Only chunks near players are loaded; a chunk is regenerated from the seed whenever it is needed.</summary>
    public sealed class Chunk
    {
        public const int Size = 32;
        public const int Shift = 5;
        public const int Area = Size * Size;

        public readonly int CX, CY;
        public readonly Tile[] Tiles = new Tile[Area];
        /// <summary>Entity id occupying each tile (0 = free). Buildings only; mobile entities are not in here.</summary>
        public readonly int[] Occupant = new int[Area];
        /// <summary>True once any tile differs from the generated base (so saves know to diff it).</summary>
        public bool TilesModified;

        public Chunk(int cx, int cy) { CX = cx; CY = cy; }

        public static long Key(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;
        public static int ChunkOf(int tile) => tile >> Shift;
        public static int LocalOf(int tile) => tile & (Size - 1);
        public static int Index(int lx, int ly) => ly * Size + lx;

        public Tile Get(int lx, int ly) => Tiles[Index(lx, ly)];
        public void Set(int lx, int ly, Tile t) { Tiles[Index(lx, ly)] = t; TilesModified = true; }
        public TilePos WorldOrigin => new TilePos(CX << Shift, CY << Shift);
    }
}
