using System.Collections.Generic;
using UnityEngine;

namespace NewGen.Art
{
    /// <summary>
    /// TEST TOOL ONLY. Add to an empty GameObject and press Play: lays out every sprite
    /// in the registry in a grid, plays animations, and zooms the main camera to fit.
    /// Click a sprite in the Hierarchy to see its name. Will be removed once real scenes exist.
    /// </summary>
    public class SpriteGalleryTest : MonoBehaviour
    {
        [SerializeField] int columns = 8;
        [SerializeField] float spacing = 0.5f;           // world units (1 unit = 1 tile = 64 px)
        [SerializeField] float framesPerSecond = 6f;

        readonly List<SpriteRenderer> animatedRenderers = new List<SpriteRenderer>();
        readonly List<Sprite[]> animatedFrames = new List<Sprite[]>();

        void Start()
        {
            SpriteRegistry registry = SpriteRegistry.Instance;
            if (registry == null) return;

            float x = 0f, rowTop = 0f, rowHeight = 0f, maxX = 0f;
            int col = 0;

            foreach (SpriteRegistry.Entry entry in registry.Entries)
            {
                if (entry == null || entry.frames == null || entry.frames.Length == 0 || entry.frames[0] == null)
                    continue;

                Sprite first = entry.frames[0];
                Bounds b = first.bounds; // local bounds, depends on pivot

                var go = new GameObject((entry.isPlaceholder ? "[placeholder] " : "[YOURS] ") + entry.key);
                go.transform.SetParent(transform, false);
                // Put the sprite's top-left corner at the top-left of its grid cell, whatever its pivot.
                go.transform.localPosition = new Vector3(x - b.min.x, rowTop - b.max.y, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = first;

                if (entry.frames.Length > 1)
                {
                    animatedRenderers.Add(sr);
                    animatedFrames.Add(entry.frames);
                }

                x += b.size.x + spacing;
                maxX = Mathf.Max(maxX, x);
                rowHeight = Mathf.Max(rowHeight, b.size.y);

                if (++col >= columns)
                {
                    col = 0;
                    x = 0f;
                    rowTop -= rowHeight + spacing;
                    rowHeight = 0f;
                }
            }

            float totalHeight = -(rowTop - rowHeight);
            FitCamera(new Vector2(maxX, totalHeight));
        }

        void FitCamera(Vector2 size)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            Vector3 origin = transform.position;
            cam.transform.position = new Vector3(origin.x + size.x * 0.5f, origin.y - size.y * 0.5f, cam.transform.position.z);
            float byHeight = size.y * 0.5f + 1f;
            float byWidth = (size.x * 0.5f + 1f) / Mathf.Max(0.01f, cam.aspect);
            cam.orthographicSize = Mathf.Max(byHeight, byWidth);
        }

        void Update()
        {
            int frame = Mathf.FloorToInt(Time.time * framesPerSecond);
            for (int i = 0; i < animatedRenderers.Count; i++)
            {
                Sprite[] frames = animatedFrames[i];
                animatedRenderers[i].sprite = frames[frame % frames.Length];
            }
        }
    }
}
