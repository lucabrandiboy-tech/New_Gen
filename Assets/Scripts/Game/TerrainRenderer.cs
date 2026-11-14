using System.Collections.Generic;
using NewGen.Art;
using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.World;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityTile = UnityEngine.Tilemaps.Tile;   // NewGen.Sim.World.Tile is the map tile; this is the tilemap asset

namespace NewGen.Game
{
    /// <summary>
    /// Draws terrain + resource patches with two Tilemaps, one chunk at a time, only around the camera.
    /// Terrain sprites come in 8x8 blocks (tile_&lt;type&gt;_01..64, numbered from the top-left of the block)
    /// and are picked by world position so neighbouring tiles continue each other.
    /// </summary>
    public sealed class TerrainRenderer : MonoBehaviour
    {
        GameRoot root;
        Tilemap terrain, resources;
        readonly Dictionary<string, TileBase> tileCache = new Dictionary<string, TileBase>();
        readonly HashSet<long> drawn = new HashSet<long>();
        readonly List<long> toClear = new List<long>();
        const int Margin = 1;   // chunks beyond the view to keep drawn

        public static TerrainRenderer Create(GameRoot root)
        {
            var go = new GameObject("Terrain");
            var r = go.AddComponent<TerrainRenderer>();
            r.root = root;
            go.AddComponent<Grid>();
            r.terrain = MakeLayer(go.transform, "Ground", -1000);
            r.resources = MakeLayer(go.transform, "Resources", -999);
            return r;
        }

        static Tilemap MakeLayer(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            tr.mode = TilemapRenderer.Mode.Chunk;
            return tm;
        }

        public void Clear()
        {
            terrain.ClearAllTiles(); resources.ClearAllTiles(); drawn.Clear();
        }

        public void Refresh()
        {
            if (root.Sim == null) return;
            var rect = root.Camera.VisibleRect(Chunk.Size);
            int cx0 = Mathf.FloorToInt(rect.xMin / Chunk.Size) - Margin, cx1 = Mathf.FloorToInt(rect.xMax / Chunk.Size) + Margin;
            int cy0 = Mathf.FloorToInt(rect.yMin / Chunk.Size) - Margin, cy1 = Mathf.FloorToInt(rect.yMax / Chunk.Size) + Margin;
            int budget = 2;   // chunks drawn per frame
            for (int cy = cy0; cy <= cy1 && budget > 0; cy++)
                for (int cx = cx0; cx <= cx1 && budget > 0; cx++)
                {
                    long key = Chunk.Key(cx, cy);
                    if (drawn.Contains(key)) continue;
                    DrawChunk(cx, cy); budget--;
                }
            toClear.Clear();
            foreach (long key in drawn)
            {
                int cx = (int)(key >> 32), cy = (int)(key & 0xFFFFFFFF);
                if (cx < cx0 - 2 || cx > cx1 + 2 || cy < cy0 - 2 || cy > cy1 + 2) toClear.Add(key);
            }
            foreach (long key in toClear) { ClearChunk((int)(key >> 32), (int)(key & 0xFFFFFFFF)); drawn.Remove(key); }
        }

        void DrawChunk(int cx, int cy)
        {
            var sim = root.Sim;
            var ground = new TileBase[Chunk.Area];
            var res = new TileBase[Chunk.Area];
            int ox = cx << Chunk.Shift, oy = cy << Chunk.Shift;
            for (int ly = 0; ly < Chunk.Size; ly++)
                for (int lx = 0; lx < Chunk.Size; lx++)
                {
                    int x = ox + lx, y = oy + ly;
                    var t = sim.World.GetTile(new TilePos(x, y));
                    int i = Chunk.Index(lx, ly);
                    ground[i] = TerrainTile(sim.Db.Terrain(t.Terrain).Id, x, y);
                    res[i] = t.Resource == 0 ? null : GetTile(sim.Db.Resource(t.Resource).SpriteKey);
                }
            var bounds = new BoundsInt(ox, oy, 0, Chunk.Size, Chunk.Size, 1);
            terrain.SetTilesBlock(bounds, ground);
            resources.SetTilesBlock(bounds, res);
            drawn.Add(Chunk.Key(cx, cy));
        }

        void ClearChunk(int cx, int cy)
        {
            var bounds = new BoundsInt(cx << Chunk.Shift, cy << Chunk.Shift, 0, Chunk.Size, Chunk.Size, 1);
            var empty = new TileBase[Chunk.Area];
            terrain.SetTilesBlock(bounds, empty);
            resources.SetTilesBlock(bounds, empty);
        }

        /// <summary>Redraw one tile (after mining changes it, etc.).</summary>
        public void RedrawTile(TilePos p)
        {
            if (root.Sim == null || !drawn.Contains(Chunk.Key(Chunk.ChunkOf(p.X), Chunk.ChunkOf(p.Y)))) return;
            var t = root.Sim.World.GetTile(p);
            var cell = new Vector3Int(p.X, p.Y, 0);
            terrain.SetTile(cell, TerrainTile(root.Sim.Db.Terrain(t.Terrain).Id, p.X, p.Y));
            resources.SetTile(cell, t.Resource == 0 ? null : GetTile(root.Sim.Db.Resource(t.Resource).SpriteKey));
        }

        TileBase TerrainTile(string terrainId, int x, int y)
        {
            // Block rows are numbered from the top; world y grows upward.
            int row = 7 - Hash.FloorMod(y, 8), col = Hash.FloorMod(x, 8);
            int nn = row * 8 + col + 1;
            var tile = GetTile($"tile_{terrainId}_{nn:00}", false);
            return tile ?? GetTile($"tile_{terrainId}_01");
        }

        TileBase GetTile(string key, bool warn = true)
        {
            if (tileCache.TryGetValue(key, out var cached)) return cached;
            var sprite = warn ? SpriteRegistry.Get(key) : SpriteRegistry.TryGet(key);
            UnityTile tile = null;
            if (sprite != null)
            {
                tile = ScriptableObject.CreateInstance<UnityTile>();
                tile.sprite = sprite;
                tile.colliderType = UnityTile.ColliderType.None;
            }
            tileCache[key] = tile;
            return tile;
        }
    }
}
