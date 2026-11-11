namespace NewGen.Sim.Core
{
    /// <summary>
    /// Deterministic random numbers (xoshiro128**). Same seed = same sequence on every PC.
    /// The world generator uses stateless hashes (Hash.*) so any chunk can be generated in any order.
    /// </summary>
    public sealed class Rng
    {
        uint s0, s1, s2, s3;

        public Rng(ulong seed)
        {
            // SplitMix64 to spread the seed.
            ulong z = seed;
            s0 = (uint)Next64(ref z); s1 = (uint)Next64(ref z); s2 = (uint)Next64(ref z); s3 = (uint)Next64(ref z);
            if ((s0 | s1 | s2 | s3) == 0) s0 = 1;
        }

        static ulong Next64(ref ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            ulong x = z;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        public uint NextUInt()
        {
            uint result = Rotl(s1 * 5, 7) * 9;
            uint t = s1 << 9;
            s2 ^= s0; s3 ^= s1; s1 ^= s2; s0 ^= s3; s2 ^= t; s3 = Rotl(s3, 11);
            return result;
        }

        /// <summary>[0, maxExclusive)</summary>
        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextUInt() % (uint)maxExclusive);
        /// <summary>[min, maxExclusive)</summary>
        public int Range(int min, int maxExclusive) => min + Next(maxExclusive - min);
        /// <summary>true with probability permille/1000.</summary>
        public bool Chance(int permille) => Next(1000) < permille;

        public uint[] GetState() => new[] { s0, s1, s2, s3 };
        public void SetState(uint[] st) { s0 = st[0]; s1 = st[1]; s2 = st[2]; s3 = st[3]; }
    }

    /// <summary>Stateless integer hashing for world generation.</summary>
    public static class Hash
    {
        public static uint U32(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
            return x;
        }

        /// <summary>Hash of (seed, x, y, salt) -> uint.</summary>
        public static uint At(ulong seed, int x, int y, uint salt = 0)
        {
            uint h = (uint)seed ^ (uint)(seed >> 32);
            h = U32(h + 0x9E3779B9u * (uint)x);
            h = U32(h ^ (0x85EBCA6Bu * (uint)y));
            h = U32(h + salt * 0xC2B2AE35u);
            return h;
        }

        /// <summary>[0,1) in 1/65536 steps.</summary>
        public static int Unit16(ulong seed, int x, int y, uint salt = 0) => (int)(At(seed, x, y, salt) & 0xFFFF);

        /// <summary>Smooth value noise in [0, 65536). cellSize in tiles. Deterministic and chunk-order independent.</summary>
        public static int ValueNoise16(ulong seed, int x, int y, int cellSize, uint salt)
        {
            int cx = FloorDiv(x, cellSize), cy = FloorDiv(y, cellSize);
            int fx = x - cx * cellSize, fy = y - cy * cellSize;
            // smoothstep weights in 0..1024
            int wx = Smooth1024(fx * 1024 / cellSize), wy = Smooth1024(fy * 1024 / cellSize);
            int a = Unit16(seed, cx, cy, salt), b = Unit16(seed, cx + 1, cy, salt);
            int c = Unit16(seed, cx, cy + 1, salt), d = Unit16(seed, cx + 1, cy + 1, salt);
            int top = a + ((b - a) * wx >> 10);
            int bot = c + ((d - c) * wx >> 10);
            return top + ((bot - top) * wy >> 10);
        }

        /// <summary>Several octaves of value noise, result in [0, 65536).</summary>
        public static int Fbm16(ulong seed, int x, int y, int baseCell, int octaves, uint salt)
        {
            long sum = 0, wsum = 0; int cell = baseCell; long w = 1024;
            for (int i = 0; i < octaves && cell >= 2; i++)
            {
                sum += ValueNoise16(seed, x, y, cell, salt + (uint)i * 31) * w;
                wsum += w; cell /= 2; w /= 2;
            }
            return wsum == 0 ? 0 : (int)(sum / wsum);
        }

        static int Smooth1024(int t) { long tt = t; return (int)(tt * tt * (3 * 1024 - 2 * tt) / (1024L * 1024)); }

        public static int FloorDiv(int a, int b) { int q = a / b; if ((a % b != 0) && ((a < 0) != (b < 0))) q--; return q; }
        public static int FloorMod(int a, int b) { int m = a % b; return m < 0 ? m + b : m; }
    }
}
