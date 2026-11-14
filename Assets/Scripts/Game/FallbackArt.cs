using System.Collections.Generic;
using UnityEngine;

namespace NewGen.Game
{
    /// <summary>
    /// Last-resort stand-in art, built in memory at run time. If a sprite is missing for any reason
    /// (not drawn yet, placeholders never generated, a typo in a name) the game draws a plain coloured
    /// box of the right size instead of nothing at all. Nothing is ever invisible.
    /// </summary>
    public static class FallbackArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public const int PixelsPerTile = 64;

        /// <summary>A flat box with a darker 1 px edge. Size in pixels; pivot bottom-left or centre.</summary>
        public static Sprite Box(int widthPx, int heightPx, Color32 fill, bool bottomLeftPivot)
        {
            widthPx = Mathf.Max(1, widthPx); heightPx = Mathf.Max(1, heightPx);
            string key = $"{widthPx}x{heightPx}:{fill.r},{fill.g},{fill.b},{fill.a}:{(bottomLeftPivot ? 'b' : 'c')}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var edge = new Color32((byte)(fill.r / 2), (byte)(fill.g / 2), (byte)(fill.b / 2), fill.a);
            var px = new Color32[widthPx * heightPx];
            for (int y = 0; y < heightPx; y++)
                for (int x = 0; x < widthPx; x++)
                    px[y * widthPx + x] = (x == 0 || y == 0 || x == widthPx - 1 || y == heightPx - 1) ? edge : fill;

            var tex = new Texture2D(widthPx, heightPx, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, widthPx, heightPx),
                bottomLeftPivot ? Vector2.zero : new Vector2(0.5f, 0.5f), PixelsPerTile);
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>Box sized to a building footprint, in tiles, plus the usual half-tile of height.</summary>
        public static Sprite Building(int tilesWide, int tilesHigh, Color32 fill)
            => Box(tilesWide * PixelsPerTile, tilesHigh * PixelsPerTile + PixelsPerTile / 2, fill, true);

        public static Color32 ColorFor(Sim.Content.BuildingKind kind)
        {
            switch (kind)
            {
                case Sim.Content.BuildingKind.Generator: return new Color32(210, 150, 60, 255);
                case Sim.Content.BuildingKind.Drill: return new Color32(140, 150, 170, 255);
                case Sim.Content.BuildingKind.Crafter: return new Color32(170, 120, 110, 255);
                case Sim.Content.BuildingKind.DataCenter: return new Color32(80, 170, 190, 255);
                case Sim.Content.BuildingKind.Chest: return new Color32(160, 140, 90, 255);
                case Sim.Content.BuildingKind.Tube: return new Color32(150, 90, 200, 255);
                case Sim.Content.BuildingKind.Wire: return new Color32(90, 140, 220, 255);
                case Sim.Content.BuildingKind.Arm: return new Color32(200, 110, 170, 255);
                default: return new Color32(130, 130, 140, 255);
            }
        }
    }
}
