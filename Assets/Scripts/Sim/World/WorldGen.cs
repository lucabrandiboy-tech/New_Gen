using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.World
{
    /// <summary>
    /// Where the coarse biome of a tile comes from. The Milestone-1 test map computes it from noise;
    /// the full Earth will read a painted biome image instead (same interface, nothing else changes).
    /// </summary>
    public interface IBiomeSource
    {
        /// <summary>Terrain index for a tile.</summary>
        byte TerrainAt(int x, int y);
    }

    /// <summary>Small Antarctica test map: snow plateau, blue-ice fields, rock outcrops, ocean past the edge.</summary>
    public sealed class AntarcticaTestBiomes : IBiomeSource
    {
        readonly ulong seed; readonly int radiusTiles;
        readonly byte snow, ice, rock, ocean;

        public AntarcticaTestBiomes(ContentDatabase db, ulong seed, int radiusChunks)
        {
            this.seed = seed; radiusTiles = radiusChunks * Chunk.Size;
            snow = db.TerrainById("snow")?.Index ?? 0;
            ice = db.TerrainById("ice")?.Index ?? 0;
            rock = db.TerrainById("rock")?.Index ?? 0;
            ocean = db.TerrainById("ocean")?.Index ?? 0;
        }

        public byte TerrainAt(int x, int y)
        {
            // Coastline: a noisy circle around the origin. Everything beyond is ocean.
            long d2 = (long)x * x + (long)y * y;
            int coastNoise = Hash.Fbm16(seed, x, y, 96, 3, 11) - 32768;           // -32768..32767
            long limit = radiusTiles + coastNoise / 1200;                           // +-27 tiles of wobble
            if (d2 > limit * limit) return ocean;

            int rockN = Hash.Fbm16(seed, x, y, 128, 4, 1);
            if (rockN > 44500) return rock;                                         // ~8 % outcrops
            int iceN = Hash.Fbm16(seed, x + 7919, y - 104729, 160, 3, 2);
            if (iceN > 40500) return ice;                                           // ~18 % ice fields
            return snow;
        }
    }

    /// <summary>
    /// Stateless generator: the same (seed, chunk) always gives the same tiles, in any order, so nothing
    /// generated is ever saved. Resource patches sit on a jittered grid so they can cross chunk borders.
    /// </summary>
    public sealed class WorldGen
    {
        public const int PatchCell = 24;          // one potential patch per 24x24 tile cell
        public const int StartClearRadius = 7;    // no resources this close to the crash site
        public const int StartPatchRing = 16;     // guaranteed starter patches at this distance

        readonly ContentDatabase db; readonly ulong seed; readonly IBiomeSource biomes;
        readonly int totalWeight;

        public WorldGen(ContentDatabase db, ulong seed, IBiomeSource biomes)
        {
            this.db = db; this.seed = seed; this.biomes = biomes;
            for (int i = 1; i < db.Resources.Count; i++) totalWeight += db.Resources[i].SpawnWeight;
        }

        public ulong Seed => seed;

        public void Generate(Chunk c)
        {
            int ox = c.CX << Chunk.Shift, oy = c.CY << Chunk.Shift;
            for (int ly = 0; ly < Chunk.Size; ly++)
                for (int lx = 0; lx < Chunk.Size; lx++)
                {
                    int x = ox + lx, y = oy + ly;
                    byte terrain = biomes.TerrainAt(x, y);
                    byte res = ResourceAt(x, y, terrain);
                    c.Tiles[Chunk.Index(lx, ly)] = new Tile(terrain, res);
                }
            c.TilesModified = false;
        }

        public Tile GenerateTile(int x, int y)
        {
            byte terrain = biomes.TerrainAt(x, y);
            return new Tile(terrain, ResourceAt(x, y, terrain));
        }

        byte ResourceAt(int x, int y, byte terrain)
        {
            var tdef = db.Terrain(terrain);
            if (!tdef.Buildable) return 0;
            if ((long)x * x + (long)y * y <= StartClearRadius * StartClearRadius) return 0;

            // Starter patches: one of each resource around the crash site, so the first playable always works.
            int n = db.Resources.Count - 1;
            for (int i = 0; i < n; i++)
            {
                // place around a ring at 45-degree steps
                int angleStep = i * 2; // 0,2,4,6 of 8 directions -> N, E, S, W
                int px = angleStep == 2 ? StartPatchRing : angleStep == 6 ? -StartPatchRing : 0;
                int py = angleStep == 0 ? StartPatchRing : angleStep == 4 ? -StartPatchRing : 0;
                if (InPatch(x, y, px, py, 5, (uint)(1000 + i))) return (byte)(i + 1);
            }

            int cx = Hash.FloorDiv(x, PatchCell), cy = Hash.FloorDiv(y, PatchCell);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = cx + dx, gy = cy + dy;
                    uint h = Hash.At(seed, gx, gy, 77);
                    if ((h & 0xFF) >= 72) continue;                                   // ~28 % of cells have a patch
                    int px = gx * PatchCell + (int)((h >> 8) % PatchCell);
                    int py = gy * PatchCell + (int)((h >> 16) % PatchCell);
                    if ((long)px * px + (long)py * py < (StartPatchRing + 10) * (StartPatchRing + 10)) continue; // keep the start tidy
                    byte which = PickResource(h >> 24);
                    var rdef = db.Resource(which);
                    int radius = rdef.PatchRadiusMin + (int)(Hash.At(seed, gx, gy, 78) % (uint)(rdef.PatchRadiusMax - rdef.PatchRadiusMin + 1));
                    if (InPatch(x, y, px, py, radius, (uint)(gx * 131 + gy))) return which;
                }
            return 0;
        }

        bool InPatch(int x, int y, int px, int py, int radius, uint salt)
        {
            int dx = x - px, dy = y - py;
            long d2 = (long)dx * dx + (long)dy * dy;
            if (d2 > (long)(radius + 2) * (radius + 2)) return false;
            // ragged edge
            int edge = Hash.Fbm16(seed, x, y, 6, 2, salt) / 16384;  // 0..3
            int r = radius - 1 + edge - 1;
            return d2 <= (long)r * r;
        }

        byte PickResource(uint roll)
        {
            if (totalWeight <= 0) return 0;
            int pick = (int)(roll % (uint)totalWeight);
            for (int i = 1; i < db.Resources.Count; i++)
            {
                pick -= db.Resources[i].SpawnWeight;
                if (pick < 0) return (byte)i;
            }
            return (byte)(db.Resources.Count - 1);
        }
    }
}
