using NewGen.Sim.Core;
using NewGen.Sim.World;

namespace NewGen.SimTest
{
    public static class WorldTests
    {
        public static World MakeWorld(ulong seed = 2026)
        {
            var db = ContentTests.Db;
            var gen = new WorldGen(db, seed, new AntarcticaTestBiomes(db, seed, db.Balance.TestMapRadiusChunks));
            return new World(db, gen);
        }

        public static void Run(string repoRoot)
        {
            Check.Section("World");
            var w = MakeWorld();
            var a = w.GetChunk(0, 0);
            var b = new Chunk(0, 0); w.Gen.Generate(b);
            bool same = true;
            for (int i = 0; i < Chunk.Area; i++) if (!a.Tiles[i].Equals(b.Tiles[i])) same = false;
            Check.True(same, "generation is deterministic");

            // Origin clear of resources, on buildable ground
            Check.Eq(0, (int)w.GetTile(new TilePos(0, 0)).Resource, "no resource at the crash site");
            Check.True(w.TerrainAt(new TilePos(0, 0)).Buildable, "crash site is buildable");

            // Starter patches exist around the ring
            int found = 0;
            for (int r = 1; r <= 4; r++)
            {
                bool any = false;
                for (int y = -30; y <= 30 && !any; y++)
                    for (int x = -30; x <= 30 && !any; x++)
                        if (w.GetTile(new TilePos(x, y)).Resource == r) any = true;
                if (any) found++;
            }
            Check.Eq(4, found, "all 4 starter resources within 30 tiles");

            // Tiles read through chunks and through GenerateTile agree
            var p = new TilePos(100, -200);
            Check.True(w.GetTile(p).Equals(w.Gen.GenerateTile(p.X, p.Y)), "unloaded read == generated");
            w.ChunkAt(p);
            Check.True(w.GetTile(p).Equals(w.Gen.GenerateTile(p.X, p.Y)), "loaded read == generated");

            // Chunk neighbours: a patch crossing a border is consistent (compare by regenerating per-tile)
            bool consistent = true;
            for (int x = 28; x < 36 && consistent; x++)
                for (int y = -4; y < 4; y++)
                    if (!w.GetTile(new TilePos(x, y)).Equals(w.Gen.GenerateTile(x, y))) { consistent = false; break; }
            Check.True(consistent, "tiles across chunk borders match");

            // Ocean beyond the edge
            int edge = w.Db.Balance.TestMapRadiusChunks * Chunk.Size + 60;
            Check.Eq("ocean", w.TerrainAt(new TilePos(edge, 0)).Id, "ocean past the edge");
            Check.True(w.TerrainAt(new TilePos(edge, 0)).Walkable == false, "ocean not walkable");

            // Terrain mix is sane in the middle
            int snow = 0, ice = 0, rock = 0, total = 0;
            for (int cy = -2; cy < 2; cy++)
                for (int cx = -2; cx < 2; cx++)
                {
                    var c = w.GetChunk(cx, cy);
                    foreach (var t in c.Tiles) { total++; string id = w.Db.Terrain(t.Terrain).Id; if (id == "snow") snow++; else if (id == "ice") ice++; else if (id == "rock") rock++; }
                }
            Check.True(snow > total / 2, $"mostly snow ({snow}/{total})");
            Check.True(ice > 0 && rock > 0, $"some ice ({ice}) and rock ({rock})");

            // Delta: untouched chunk has none; modified chunk reports only the change
            Check.Eq(0, w.Delta(a).Count, "untouched chunk has no delta");
            var t0 = w.GetTile(new TilePos(3, 3));
            w.SetTile(new TilePos(3, 3), new Tile(t0.Terrain, 0));
            w.SetTile(new TilePos(5, 5), new Tile(2, 0));
            var d = w.Delta(a);
            Check.True(d.Count >= 1 && d.Count <= 2, "delta only lists changed tiles: " + d.Count);
            var w2 = MakeWorld();
            w2.ApplyDelta(0, 0, d);
            Check.True(w2.GetTile(new TilePos(5, 5)).Equals(new Tile(2, 0)), "delta round trip");

            // Occupancy
            w.SetOccupant(new TilePos(-1, -1), 42);
            Check.Eq(42, w.OccupantAt(new TilePos(-1, -1)), "occupant set/get across negative chunk");
            Check.Eq(0, w.OccupantAt(new TilePos(999, 999)), "unloaded occupant is 0");
        }
    }
}
