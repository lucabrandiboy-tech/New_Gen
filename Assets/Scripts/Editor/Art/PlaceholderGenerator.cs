using System.Collections.Generic;
using System.IO;
using NewGen.Art;
using UnityEditor;
using UnityEngine;

namespace NewGen.EditorTools.Art
{
    /// <summary>
    /// Writes flat-colour placeholder PNGs for every entry in SpriteManifest
    /// into Assets/Art/_Placeholders ONLY. Never touches Assets/Art/Sprites.
    /// </summary>
    public static class PlaceholderGenerator
    {
        [MenuItem("New Generation/Art/Generate Placeholders")]
        public static void Generate()
        {
            string folderOnDisk = Path.Combine(Directory.GetCurrentDirectory(), SpriteNaming.PlaceholderFolder);
            Directory.CreateDirectory(folderOnDisk);

            var expected = new HashSet<string>();
            int written = 0;

            foreach (SpriteManifest.Spec spec in SpriteManifest.All)
            {
                string fileName = spec.FileName;
                expected.Add(fileName.ToLowerInvariant());

                Texture2D tex = Draw(spec);
                try
                {
                    File.WriteAllBytes(Path.Combine(folderOnDisk, fileName), tex.EncodeToPNG());
                    written++;
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }

            // Report (but do not delete) placeholders no longer in the manifest.
            foreach (string file in Directory.GetFiles(folderOnDisk, "*.png"))
            {
                string name = Path.GetFileName(file);
                if (!expected.Contains(name.ToLowerInvariant()))
                    Debug.LogWarning($"[Art] Old placeholder not in the manifest anymore: {name} (left in place).");
            }

            AssetDatabase.Refresh(); // imports the PNGs, which triggers a registry rebuild
            Debug.Log($"[Art] Wrote {written} placeholder sprites to {SpriteNaming.PlaceholderFolder}.");
        }

        static Texture2D Draw(SpriteManifest.Spec spec)
        {
            int fw = spec.frameWidth, fh = spec.frameHeight;
            int w = fw * spec.frames;
            var px = new Color32[w * fh];

            Color32 baseCol = SpriteManifest.BaseColor(spec.kind);
            Color32 light = Shade(baseCol, 1.25f);
            Color32 dark = Shade(baseCol, 0.65f);
            Color32 outline = Shade(baseCol, 0.3f);
            Color32 marker = new Color32(255, 255, 255, 255);

            for (int f = 0; f < spec.frames; f++)
            {
                int ox = f * fw;
                for (int y = 0; y < fh; y++)
                for (int x = 0; x < fw; x++)
                {
                    Color32 c = baseCol;

                    if (spec.kind == SpriteManifest.Kind.Building)
                    {
                        // Front wall = bottom third (darker). Roof = rest. Light from top-left.
                        int wallTop = fh / 3;
                        if (y < wallTop) c = dark;
                        else if (x < 2 || y >= fh - 2) c = light;
                        if (y == wallTop) c = outline; // roof/wall edge line
                        if (spec.name.EndsWith("_damaged") && ((x / 8 + y / 8) % 4 == 0)) c = Shade(c, 0.5f);
                        if (spec.name.EndsWith("_destroyed")) c = Shade(c, 0.4f);
                    }

                    bool border = x == 0 || y == 0 || x == fw - 1 || y == fh - 1;
                    if (border && spec.kind != SpriteManifest.Kind.Terrain) c = outline;
                    if (border && spec.kind == SpriteManifest.Kind.Terrain) c = Shade(baseCol, 0.9f); // faint grid

                    px[y * w + ox + x] = c;
                }

                // Animation marker: a small white square that moves each frame.
                if (spec.frames > 1)
                {
                    int size = Mathf.Max(2, fw / 10);
                    int mx = 2 + (fw - size - 4) * f / (spec.frames - 1);
                    int my = fh - size - 3;
                    for (int y = my; y < my + size; y++)
                    for (int x = mx; x < mx + size; x++)
                        px[y * w + ox + x] = marker;
                }
            }

            var tex = new Texture2D(w, fh, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        static Color32 Shade(Color32 c, float k)
        {
            return new Color32(
                (byte)Mathf.Clamp(c.r * k, 0, 255),
                (byte)Mathf.Clamp(c.g * k, 0, 255),
                (byte)Mathf.Clamp(c.b * k, 0, 255),
                c.a);
        }
    }
}
