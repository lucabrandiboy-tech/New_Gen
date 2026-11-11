using NewGen.Sim.Core;
using UnityEngine;

namespace NewGen.Game
{
    /// <summary>Sim units ↔ Unity units. One Unity unit = one tile. Sub-positions are 1/256 tile.</summary>
    public static class WorldSpace
    {
        public static Vector3 ToUnity(SubPos p) => new Vector3(p.X / (float)Fix.SubPerTile, p.Y / (float)Fix.SubPerTile, 0f);
        public static Vector3 TileCorner(TilePos t) => new Vector3(t.X, t.Y, 0f);
        public static Vector3 TileCenter(TilePos t) => new Vector3(t.X + 0.5f, t.Y + 0.5f, 0f);
        public static SubPos ToSub(Vector3 v) => new SubPos(Mathf.FloorToInt(v.x * Fix.SubPerTile), Mathf.FloorToInt(v.y * Fix.SubPerTile));
        public static TilePos ToTile(Vector3 v) => new TilePos(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));

        /// <summary>Z rotation (degrees) so a sprite drawn facing North faces the given direction.</summary>
        public static float FacingAngle(Dir d)
        {
            switch (d) { case Dir.East: return -90f; case Dir.South: return 180f; case Dir.West: return 90f; default: return 0f; }
        }

        /// <summary>Lower on screen draws on top (3/4 view).</summary>
        public static int SortOrder(float y) => -(int)(y * 16f);
    }
}
