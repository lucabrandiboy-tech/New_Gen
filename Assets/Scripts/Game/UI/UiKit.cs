using UnityEngine;
using UnityEngine.UI;

namespace NewGen.Game.UI
{
    /// <summary>
    /// Tiny helpers to build placeholder UI from code. Everything is plain uGUI so the owner can
    /// restyle it later (colours/fonts/sprites in one place: the fields below).
    /// </summary>
    public static class UiKit
    {
        public static Font Font;
        public static Color Panel = new Color(0.07f, 0.08f, 0.1f, 0.92f);
        public static Color PanelLight = new Color(0.14f, 0.16f, 0.2f, 0.95f);
        public static Color Accent = new Color(0.35f, 0.85f, 0.6f);
        public static Color Danger = new Color(0.95f, 0.3f, 0.3f);
        public static Color TextColor = new Color(0.85f, 0.9f, 0.9f);
        public static Color Dim = new Color(0.55f, 0.6f, 0.65f);
        public static int FontSize = 20;

        public static void EnsureFont()
        {
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

        public static GameObject Panel_(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        /// <summary>Anchored box. anchor/pivot in 0..1, pos in pixels from the anchor, size in pixels.</summary>
        public static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(go);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(GameObject go, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            var rt = Rect(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Text Text_(Transform parent, string text, int size = 0, TextAnchor align = TextAnchor.UpperLeft, Color? color = null)
        {
            EnsureFont();
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = size > 0 ? size : FontSize; t.text = text; t.alignment = align;
            t.color = color ?? TextColor; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button_(Transform parent, string label, System.Action onClick, Vector2? size = null, Color? bg = null)
        {
            var go = Panel_(parent, "Button " + label, bg ?? PanelLight);
            var le = go.AddComponent<LayoutElement>();
            var sz = size ?? new Vector2(220f, 44f);
            le.preferredWidth = sz.x; le.preferredHeight = sz.y; le.minHeight = sz.y;
            Rect(go).sizeDelta = sz;
            var b = go.AddComponent<Button>();
            var colors = b.colors; colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f); colors.pressedColor = Accent; b.colors = colors;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var t = Text_(go.transform, label, 0, TextAnchor.MiddleCenter);
            Stretch(t.gameObject, 8, 8, 4, 4);
            return b;
        }

        public static Image Image_(Transform parent, Sprite sprite, Vector2 size)
        {
            var go = new GameObject("Image", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
            Rect(go).sizeDelta = size;
            var le = go.AddComponent<LayoutElement>(); le.preferredWidth = size.x; le.preferredHeight = size.y;
            return img;
        }

        public static VerticalLayoutGroup VList(GameObject go, float spacing = 6f, int pad = 10)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HList(GameObject go, float spacing = 6f, int pad = 4)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            return h;
        }

        public static GameObject Row(Transform parent, float height = 44f)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            HList(go);
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = height; le.minHeight = height;
            return go;
        }

        /// <summary>A filled bar: returns the fill image (set fillAmount 0..1).</summary>
        public static Image Bar(Transform parent, Vector2 size, Color fill, out Text label)
        {
            var bg = Panel_(parent, "Bar", new Color(0f, 0f, 0f, 0.6f));
            Rect(bg).sizeDelta = size;
            var le = bg.AddComponent<LayoutElement>(); le.preferredWidth = size.x; le.preferredHeight = size.y;
            var f = Panel_(bg.transform, "Fill", fill);
            Stretch(f, 2, 2, 2, 2);
            var img = f.GetComponent<Image>();
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal; img.fillOrigin = 0;
            img.sprite = WhiteSprite();   // Filled images need a sprite
            label = Text_(bg.transform, "", 16, TextAnchor.MiddleCenter);
            Stretch(label.gameObject);
            return img;
        }

        static Sprite white;
        public static Sprite WhiteSprite()
        {
            if (white != null) return white;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels32(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return white;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Seconds(int s) => s >= 60 ? $"{s / 60}m {s % 60:00}s" : s + "s";
        public static string Watts(long w) => w >= 1000000 ? $"{w / 1000000f:0.0} MW" : w >= 1000 ? $"{w / 1000f:0.0} kW" : w + " W";
    }
}
