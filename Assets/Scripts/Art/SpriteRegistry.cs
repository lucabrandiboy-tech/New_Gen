using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewGen.Art
{
    /// <summary>
    /// Lookup table: sprite key -> sprite frames. Built automatically in the editor
    /// (see SpriteRegistryBuilder). If the owner's file exists in Assets/Art/Sprites
    /// it wins; otherwise the placeholder from Assets/Art/_Placeholders is used.
    /// Game code only ever asks for a key, e.g. SpriteRegistry.Get("bld_smelter_idle").
    /// </summary>
    public class SpriteRegistry : ScriptableObject
    {
        /// <summary>Path inside a Resources folder (no extension).</summary>
        public const string ResourcePath = "SpriteRegistry";

        [Serializable]
        public class Entry
        {
            public string key;
            public bool isPlaceholder;
            public string sourcePath;
            public Sprite[] frames;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        Dictionary<string, Entry> lookup;
        readonly HashSet<string> warnedMissing = new HashSet<string>();

        static SpriteRegistry instance;

        public static SpriteRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<SpriteRegistry>(ResourcePath);
                    if (instance == null)
                        Debug.LogError("[SpriteRegistry] Not found. In Unity use: New Generation > Art > Rebuild Sprite Registry.");
                }
                return instance;
            }
        }

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>First frame for a key, or null (with a one-time warning) if missing.</summary>
        public static Sprite Get(string key)
        {
            Sprite[] frames = GetFrames(key);
            return frames != null && frames.Length > 0 ? frames[0] : null;
        }

        /// <summary>All frames for a key, or null if missing.</summary>
        public static Sprite[] GetFrames(string key)
        {
            SpriteRegistry reg = Instance;
            if (reg == null) return null;
            Entry e = reg.Find(key);
            return e?.frames;
        }

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (lookup == null) BuildLookup();
            string k = key.ToLowerInvariant();
            if (lookup.TryGetValue(k, out Entry e)) return e;
            if (warnedMissing.Add(k))
                Debug.LogWarning($"[SpriteRegistry] No sprite named '{k}'. Check the file name or regenerate placeholders.");
            return null;
        }

        void BuildLookup()
        {
            lookup = new Dictionary<string, Entry>(entries.Count);
            foreach (Entry e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.key)) continue;
                lookup[e.key] = e;
            }
        }

        void OnEnable()
        {
            lookup = null;
            warnedMissing.Clear();
        }

#if UNITY_EDITOR
        /// <summary>Editor only: replace all entries (used by SpriteRegistryBuilder).</summary>
        public void EditorSetEntries(List<Entry> newEntries)
        {
            entries = newEntries ?? new List<Entry>();
            lookup = null;
            warnedMissing.Clear();
        }
#endif
    }
}
