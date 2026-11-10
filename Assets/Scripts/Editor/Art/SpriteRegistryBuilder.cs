using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewGen.Art;
using UnityEditor;
using UnityEngine;

namespace NewGen.EditorTools.Art
{
    /// <summary>
    /// Rebuilds the SpriteRegistry asset whenever art in Assets/Art/Sprites or
    /// Assets/Art/_Placeholders is added, changed, moved or deleted.
    /// Owner art always beats a placeholder with the same name.
    /// </summary>
    public class SpriteRegistryBuilder : AssetPostprocessor
    {
        const string ResourcesFolder = "Assets/Data/Resources";
        const string RegistryAssetPath = ResourcesFolder + "/" + SpriteRegistry.ResourcePath + ".asset";

        static bool rebuildScheduled;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
                ScheduleRebuild();
        }

        static bool Touches(string[] paths)
        {
            foreach (string p in paths)
                if (SpriteNaming.IsInArtFolder(p) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        static void ScheduleRebuild()
        {
            if (rebuildScheduled) return;
            rebuildScheduled = true;
            // Wait until the current import finishes before loading sprites.
            EditorApplication.delayCall += () =>
            {
                rebuildScheduled = false;
                Rebuild();
            };
        }

        [MenuItem("New Generation/Art/Rebuild Sprite Registry")]
        public static void Rebuild()
        {
            SpriteRegistry registry = LoadOrCreateRegistry();
            if (registry == null) return;

            var byKey = new Dictionary<string, SpriteRegistry.Entry>();
            // Lowest priority first; later folders replace earlier ones with the same name.
            Collect(SpriteNaming.PlaceholderFolder, SpriteRegistry.Origin.Placeholder, byKey);
            Collect(SpriteNaming.GeneratedFolder, SpriteRegistry.Origin.Generated, byKey);
            int overridden = Collect(SpriteNaming.UserFolder, SpriteRegistry.Origin.Owner, byKey);

            List<SpriteRegistry.Entry> list = byKey.Values.OrderBy(e => e.key, StringComparer.Ordinal).ToList();
            registry.EditorSetEntries(list);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssetIfDirty(registry);

            int owner = list.Count(e => e.origin == SpriteRegistry.Origin.Owner);
            int generated = list.Count(e => e.origin == SpriteRegistry.Origin.Generated);
            int placeholders = list.Count(e => e.origin == SpriteRegistry.Origin.Placeholder);
            Debug.Log($"[Art] Sprite registry rebuilt: {list.Count} sprites ({owner} yours, {generated} generated, {placeholders} placeholders; {overridden} replaced by your art).");
        }

        /// <returns>How many lower-priority entries this folder replaced.</returns>
        static int Collect(string folder, SpriteRegistry.Origin origin, Dictionary<string, SpriteRegistry.Entry> byKey)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return 0;

            int replaced = 0;
            var seenInThisFolder = new HashSet<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                string key = SpriteNaming.ToKey(fileName);

                Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .OrderBy(FrameIndex)
                    .ToArray();

                if (frames.Length == 0)
                {
                    Debug.LogWarning($"[Art] '{path}' produced no sprites. It may not have finished importing; try New Generation > Art > Rebuild Sprite Registry.");
                    continue;
                }

                if (!seenInThisFolder.Add(key))
                {
                    Debug.LogWarning($"[Art] Two files in {folder} both map to the name '{key}'. Only '{path}' is used. Rename one of them.");
                }
                else if (byKey.TryGetValue(key, out SpriteRegistry.Entry existing) && existing.origin < origin)
                {
                    replaced++;
                }

                byKey[key] = new SpriteRegistry.Entry
                {
                    key = key,
                    isPlaceholder = origin == SpriteRegistry.Origin.Placeholder,
                    origin = origin,
                    sourcePath = path,
                    frames = frames,
                };
            }
            return replaced;
        }

        /// <summary>Sliced frames are named "&lt;file&gt;_&lt;index&gt;". Single sprites sort as 0.</summary>
        static int FrameIndex(Sprite s)
        {
            int underscore = s.name.LastIndexOf('_');
            if (underscore >= 0 && int.TryParse(s.name.Substring(underscore + 1), out int i)) return i;
            return 0;
        }

        static SpriteRegistry LoadOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<SpriteRegistry>(RegistryAssetPath);
            if (registry != null) return registry;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Resources");

            registry = ScriptableObject.CreateInstance<SpriteRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryAssetPath);
            AssetDatabase.SaveAssets();
            return registry;
        }
    }
}
