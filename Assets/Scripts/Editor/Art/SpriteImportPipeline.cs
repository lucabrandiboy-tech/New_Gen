using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewGen.Art;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace NewGen.EditorTools.Art
{
    /// <summary>
    /// Auto-import settings for every PNG in Assets/Art/Sprites and Assets/Art/_Placeholders:
    /// Sprite, Point filter, no compression, 64 pixels per unit, no mipmaps.
    /// Files ending in _&lt;N&gt;f (e.g. unit_player_walk_4f.png) are sliced into N equal frames, left to right.
    /// This only changes IMPORT SETTINGS; it never modifies the PNG files themselves.
    /// </summary>
    public class SpriteImportPipeline : AssetPostprocessor
    {
        // Bump this number whenever the rules below change, so Unity re-imports all art.
        const uint PipelineVersion = 1;

        public override uint GetVersion() => PipelineVersion;

        void OnPreprocessTexture()
        {
            if (!SpriteNaming.IsInArtFolder(assetPath)) return;
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (TextureImporter)assetImporter;
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (!SpriteNaming.IsValidName(fileName))
                Debug.LogWarning($"[Art] '{assetPath}': name should be lowercase letters, digits and underscores only (see ART_GUIDE.md). Importing anyway.");

            bool bottomLeft = SpriteNaming.UsesBottomLeftPivot(fileName);
            SpriteAlignment alignment = bottomLeft ? SpriteAlignment.BottomLeft : SpriteAlignment.Center;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = SpriteNaming.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 8192;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // crisp rectangles for pixel art
            settings.spriteAlignment = (int)alignment;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);

            int frames = SpriteNaming.GetFrameCount(fileName);
            if (frames <= 1)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            if (!PngHeader.TryReadSize(assetPath, out int width, out int height))
            {
                Debug.LogWarning($"[Art] '{assetPath}': could not read PNG size (is Git LFS installed? the file may be an LFS pointer). Imported as a single sprite.");
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            if (width % frames != 0)
            {
                Debug.LogWarning($"[Art] '{assetPath}': width {width}px does not divide into {frames} equal frames. Imported as a single sprite. Fix the width or the _{frames}f suffix.");
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            SliceHorizontal(importer, fileName, frames, width / frames, height, alignment);
        }

        static void SliceHorizontal(TextureImporter importer, string baseName, int frames, int frameWidth, int frameHeight, SpriteAlignment alignment)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            // Keep existing sprite IDs so references to frames survive re-imports.
            var existingIds = new Dictionary<string, GUID>();
            foreach (SpriteRect r in dataProvider.GetSpriteRects())
                if (r != null && !string.IsNullOrEmpty(r.name)) existingIds[r.name] = r.spriteID;

            Vector2 pivot = alignment == SpriteAlignment.BottomLeft ? Vector2.zero : new Vector2(0.5f, 0.5f);
            var rects = new SpriteRect[frames];
            for (int i = 0; i < frames; i++)
            {
                string name = $"{baseName}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    alignment = alignment,
                    pivot = pivot,
                    spriteID = existingIds.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                };
            }

            dataProvider.SetSpriteRects(rects);

            var nameIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIdProvider != null)
                nameIdProvider.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

            dataProvider.Apply();
        }
    }

    /// <summary>Reads width/height straight from a PNG file header (the texture isn't loaded yet during pre-import).</summary>
    static class PngHeader
    {
        static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static bool TryReadSize(string assetPath, out int width, out int height)
        {
            width = height = 0;
            try
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var header = new byte[24];
                    if (fs.Read(header, 0, 24) != 24) return false;
                    for (int i = 0; i < 8; i++)
                        if (header[i] != Signature[i]) return false;
                    // Bytes 12..15 must be "IHDR"
                    if (header[12] != 'I' || header[13] != 'H' || header[14] != 'D' || header[15] != 'R') return false;
                    width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                    height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                    return width > 0 && height > 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (System.UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
