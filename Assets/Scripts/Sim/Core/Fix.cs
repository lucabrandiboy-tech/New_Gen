namespace NewGen.Sim.Core
{
    /// <summary>
    /// Fixed-point helpers. The simulation never uses floats for positions or movement so that
    /// every PC computes exactly the same result (needed for deterministic co-op later).
    /// World positions are ints in SUB units: 1 tile = 256 sub-units.
    /// </summary>
    public static class Fix
    {
        public const int SubPerTile = 256;
        public const int SubShift = 8;

        public static int TileOf(int sub) => sub >> SubShift;              // floor, works for negatives
        public static int SubOf(int tile) => tile << SubShift;
        public static int Center(int tile) => (tile << SubShift) + SubPerTile / 2;

        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static int Abs(int v) => v < 0 ? -v : v;
        public static int Sign(int v) => v < 0 ? -1 : (v > 0 ? 1 : 0);
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;

        /// <summary>Integer square root (floor).</summary>
        public static int Sqrt(long v)
        {
            if (v <= 0) return 0;
            long r = (long)System.Math.Sqrt(v);
            while (r * r > v) r--;
            while ((r + 1) * (r + 1) <= v) r++;
            return (int)r;
        }

        /// <summary>Length of (dx,dy) in sub-units.</summary>
        public static int Length(int dx, int dy) => Sqrt((long)dx * dx + (long)dy * dy);

        /// <summary>Scale (dx,dy) to the given length (sub-units). Returns (0,0) for a zero vector.</summary>
        public static void Normalize(int dx, int dy, int length, out int ox, out int oy)
        {
            int len = Length(dx, dy);
            if (len == 0) { ox = 0; oy = 0; return; }
            ox = (int)((long)dx * length / len);
            oy = (int)((long)dy * length / len);
        }

        /// <summary>Multiply by a per-mille factor (1000 = x1).</summary>
        public static int Permille(int v, int permille) => (int)((long)v * permille / 1000);
    }

    /// <summary>Integer tile coordinate.</summary>
    public readonly struct TilePos : System.IEquatable<TilePos>
    {
        public readonly int X, Y;
        public TilePos(int x, int y) { X = x; Y = y; }
        public static TilePos operator +(TilePos a, TilePos b) => new TilePos(a.X + b.X, a.Y + b.Y);
        public static TilePos operator -(TilePos a, TilePos b) => new TilePos(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(TilePos a, TilePos b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(TilePos a, TilePos b) => !(a == b);
        public bool Equals(TilePos o) => this == o;
        public override bool Equals(object o) => o is TilePos t && t == this;
        public override int GetHashCode() => unchecked(X * 73856093 ^ Y * 19349663);
        public override string ToString() => $"({X},{Y})";
        public TilePos Offset(Dir d) => this + d.Delta();
        public int ManhattanTo(TilePos o) => Fix.Abs(X - o.X) + Fix.Abs(Y - o.Y);
    }

    /// <summary>Integer sub-tile world position (1 tile = 256).</summary>
    public readonly struct SubPos : System.IEquatable<SubPos>
    {
        public readonly int X, Y;
        public SubPos(int x, int y) { X = x; Y = y; }
        public TilePos Tile => new TilePos(Fix.TileOf(X), Fix.TileOf(Y));
        public static SubPos FromTileCenter(TilePos t) => new SubPos(Fix.Center(t.X), Fix.Center(t.Y));
        public static SubPos operator +(SubPos a, SubPos b) => new SubPos(a.X + b.X, a.Y + b.Y);
        public static SubPos operator -(SubPos a, SubPos b) => new SubPos(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(SubPos a, SubPos b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(SubPos a, SubPos b) => !(a == b);
        public bool Equals(SubPos o) => this == o;
        public override bool Equals(object o) => o is SubPos t && t == this;
        public override int GetHashCode() => unchecked(X * 73856093 ^ Y * 19349663);
        public int DistanceTo(SubPos o) => Fix.Length(X - o.X, Y - o.Y);
        public override string ToString() => $"sub({X},{Y})";
    }

    /// <summary>Four directions. Rotation order: North, East, South, West (clockwise). Y grows upward (north).</summary>
    public enum Dir : byte { North = 0, East = 1, South = 2, West = 3 }

    public static class DirExt
    {
        public static TilePos Delta(this Dir d)
        {
            switch (d)
            {
                case Dir.North: return new TilePos(0, 1);
                case Dir.East: return new TilePos(1, 0);
                case Dir.South: return new TilePos(0, -1);
                default: return new TilePos(-1, 0);
            }
        }
        public static Dir Opposite(this Dir d) => (Dir)(((int)d + 2) & 3);
        public static Dir RotateCW(this Dir d) => (Dir)(((int)d + 1) & 3);
        public static Dir RotateCCW(this Dir d) => (Dir)(((int)d + 3) & 3);
        /// <summary>Rotate a local offset (defined for North) into world space for facing d.</summary>
        public static TilePos Rotate(this Dir d, TilePos local)
        {
            switch (d)
            {
                case Dir.North: return local;
                case Dir.East: return new TilePos(local.Y, -local.X);
                case Dir.South: return new TilePos(-local.X, -local.Y);
                default: return new TilePos(-local.Y, local.X);
            }
        }
        public static Dir FromDelta(int dx, int dy)
        {
            if (Fix.Abs(dx) >= Fix.Abs(dy)) return dx >= 0 ? Dir.East : Dir.West;
            return dy >= 0 ? Dir.North : Dir.South;
        }
        public static string Name(this Dir d)
        {
            switch (d) { case Dir.North: return "up"; case Dir.East: return "right"; case Dir.South: return "down"; default: return "left"; }
        }
    }
}
