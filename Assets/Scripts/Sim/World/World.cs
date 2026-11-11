using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.World
{
    /// <summary>Loaded chunks + tile access. Chunks are created from the generator on first touch.</summary>
    public sealed class World
    {
        public readonly ContentDatabase Db;
        public readonly WorldGen Gen;
        readonly Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();

        public World(ContentDatabase db, WorldGen gen) { Db = db; Gen = gen; }

        public IEnumerable<Chunk> LoadedChunks => chunks.Values;
        public int LoadedChunkCount => chunks.Count;

        public Chunk GetChunk(int cx, int cy, bool create = true)
        {
            long key = Chunk.Key(cx, cy);
            if (chunks.TryGetValue(key, out var c)) return c;
            if (!create) return null;
            c = new Chunk(cx, cy);
            Gen.Generate(c);
            chunks[key] = c;
            return c;
        }

        public Chunk ChunkAt(TilePos p, bool create = true) => GetChunk(Chunk.ChunkOf(p.X), Chunk.ChunkOf(p.Y), create);

        public bool IsLoaded(int cx, int cy) => chunks.ContainsKey(Chunk.Key(cx, cy));

        public void Unload(int cx, int cy) => chunks.Remove(Chunk.Key(cx, cy));

        public Tile GetTile(TilePos p)
        {
            var c = ChunkAt(p, false);
            if (c != null) return c.Get(Chunk.LocalOf(p.X), Chunk.LocalOf(p.Y));
            return Gen.GenerateTile(p.X, p.Y);
        }

        public void SetTile(TilePos p, Tile t)
        {
            var c = ChunkAt(p);
            c.Set(Chunk.LocalOf(p.X), Chunk.LocalOf(p.Y), t);
        }

        public TerrainDef TerrainAt(TilePos p) => Db.Terrain(GetTile(p).Terrain);
        public ResourceDef ResourceAt(TilePos p) => Db.Resource(GetTile(p).Resource);

        public int OccupantAt(TilePos p)
        {
            var c = ChunkAt(p, false);
            return c == null ? 0 : c.Occupant[Chunk.Index(Chunk.LocalOf(p.X), Chunk.LocalOf(p.Y))];
        }

        public void SetOccupant(TilePos p, int entityId)
        {
            var c = ChunkAt(p);
            c.Occupant[Chunk.Index(Chunk.LocalOf(p.X), Chunk.LocalOf(p.Y))] = entityId;
        }

        /// <summary>Tiles that differ from the generated base, for saving. Returns (index, tile) pairs.</summary>
        public List<KeyValuePair<int, Tile>> Delta(Chunk c)
        {
            var list = new List<KeyValuePair<int, Tile>>();
            if (!c.TilesModified) return list;
            var fresh = new Chunk(c.CX, c.CY);
            Gen.Generate(fresh);
            for (int i = 0; i < Chunk.Area; i++)
                if (!c.Tiles[i].Equals(fresh.Tiles[i])) list.Add(new KeyValuePair<int, Tile>(i, c.Tiles[i]));
            return list;
        }

        public void ApplyDelta(int cx, int cy, List<KeyValuePair<int, Tile>> delta)
        {
            var c = GetChunk(cx, cy);
            foreach (var kv in delta) c.Tiles[kv.Key] = kv.Value;
            c.TilesModified = delta.Count > 0;
        }
    }
}
