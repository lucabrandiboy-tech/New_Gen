// Minimal stand-ins for the Unity APIs this project uses, so the Unity-facing code
// (Assets/Scripts/Game and Assets/Scripts/Art) can be COMPILE-CHECKED without Unity.
//
// WHAT THIS CATCHES: typos, wrong member names on our own classes, ambiguous types,
// missing usings, wrong argument counts, bad overrides — i.e. most of our own mistakes.
// WHAT IT DOES NOT CATCH: using a real Unity API incorrectly (a stub signature here can
// differ from Unity's). Only Unity itself proves the project compiles.
//
// If Unity reports an error that this harness missed, fix the stub too so it is caught next time.
#pragma warning disable CS0649, CS0067, CS8632, IDE0060
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public static Vector2 one => new Vector2(1, 1);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float f) => new Vector2(a.x * f, a.y * f);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z = 0f) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public static Vector3 one => new Vector3(1, 1, 1);
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0f);
    }

    public struct Vector3Int
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public struct BoundsInt
    {
        public BoundsInt(int x, int y, int z, int sx, int sy, int sz) { }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMin => x; public float xMax => x + width;
        public float yMin => y; public float yMax => y + height;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color clear => new Color(0, 0, 0, 0);
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new Color32(0, 0, 0, 255);
    }

    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.0174533f;
        public static float Clamp(float v, float a, float b) => v;
        public static int Clamp(int v, int a, int b) => v;
        public static float Clamp01(float v) => v;
        public static float Lerp(float a, float b, float t) => a;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int FloorToInt(float f) => (int)f;
        public static int RoundToInt(float f) => (int)f;
        public static int CeilToInt(float f) => (int)f;
        public static float Abs(float f) => f < 0 ? -f : f;
        public static float Sin(float f) => 0f;
        public static float Cos(float f) => 0f;
        public static float Atan2(float y, float x) => 0f;
        public static float Exp(float f) => 0f;
        public static float Sqrt(float f) => 0f;
        public static bool Approximately(float a, float b) => a == b;
    }

    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object o) { }
        public static void DestroyImmediate(Object o) { }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : Object => new T[0];
        public static implicit operator bool(Object o) => true;
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => 0;
    }

    public enum FindObjectsSortMode { None, InstanceID }

    public class Component : Object
    {
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public string tag { get; set; }
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T AddComponent<T>() where T : Component => default;
    }

    public class Behaviour : Component { public bool enabled { get; set; } }
    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get; }
        public string tag { get; set; }
        public void SetActive(bool v) { }
        public bool activeSelf { get; }
        public T AddComponent<T>() where T : Component => default;
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public int childCount { get; }
        public Transform parent { get; set; }
        public Transform GetChild(int i) => null;
        public Transform Find(string path) => null;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public IEnumerator GetEnumerator() => null;
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public class Camera : Behaviour
    {
        public static Camera main { get; }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float aspect { get; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Vector3 ScreenToWorldPoint(Vector3 p) => default;
    }

    public class Texture2D : Object
    {
        public Texture2D(int w, int h, TextureFormat f, bool mipmaps) { }
        public void SetPixels32(Color32[] px) { }
        public void Apply() { }
        public void Apply(bool a, bool b) { }
    }

    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Clamp, Repeat }

    public class Sprite : Object
    {
        public Bounds bounds { get; }
        public static Sprite Create(Texture2D tex, Rect rect, Vector2 pivot, float ppu) => null;
    }

    public struct Bounds
    {
        public Vector3 min { get; }
        public Vector3 max { get; }
        public Vector3 size { get; }
    }

    public class Renderer : Component
    {
        public int sortingOrder { get; set; }
        public bool enabled { get; set; }
    }

    public class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; }
        public Color color { get; set; }
        public bool flipX { get; set; }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => default;
    }

    public class Font : Object { }

    public static class ImageConversion
    {
        public static byte[] EncodeToPNG(this Texture2D tex) => new byte[0];
    }

    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class SerializableAttribute2 : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.All)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => default;
        public static T GetBuiltinResource<T>(string path) where T : Object => default;
    }

    public static class PlayerPrefs
    {
        public static void SetString(string k, string v) { }
        public static string GetString(string k, string def = "") => def;
        public static void SetFloat(string k, float v) { }
        public static float GetFloat(string k, float def = 0f) => def;
        public static void DeleteKey(string k) { }
        public static void Save() { }
    }

    public static class Application
    {
        public static string persistentDataPath => "";
        public static string streamingAssetsPath => "";
        public static bool isEditor => false;
        public static void Quit() { }
    }

    public static class Time
    {
        public static float deltaTime => 0f;
        public static float unscaledDeltaTime => 0f;
        public static float time => 0f;
        public static float unscaledTime => 0f;
    }

    public static class Debug
    {
        public static bool isDebugBuild => false;
        public static void Log(object o) { }
        public static void LogWarning(object o) { }
        public static void LogError(object o) { }
    }

    public static class Random
    {
        public static int Range(int min, int max) => min;
        public static float Range(float min, float max) => min;
    }

    public class Grid : Behaviour { }

    public class RectOffset
    {
        public RectOffset(int l, int r, int t, int b) { }
    }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Canvas : Behaviour { public RenderMode renderMode { get; set; } }
    public class AudioListener : Behaviour { }
}

namespace UnityEngine.Tilemaps
{
    public abstract class TileBase : ScriptableObject { }

    public class Tile : TileBase
    {
        public enum ColliderType { None, Sprite, Grid }
        public Sprite sprite { get; set; }
        public ColliderType colliderType { get; set; }
        public Color color { get; set; }
    }

    public class Tilemap : Behaviour
    {
        public void SetTile(Vector3Int pos, TileBase tile) { }
        public void SetTilesBlock(BoundsInt bounds, TileBase[] tiles) { }
        public void ClearAllTiles() { }
    }

    public class TilemapRenderer : Renderer
    {
        public enum Mode { Chunk, Individual }
        public new Mode mode { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public class Light2D : Behaviour
    {
        public enum LightType { Parametric, Freeform, Sprite, Point, Global }
        public LightType lightType { get; set; }
        public float intensity { get; set; }
        public Color color { get; set; }
        public float pointLightInnerRadius { get; set; }
        public float pointLightOuterRadius { get; set; }
    }

    public class UniversalAdditionalCameraData : Behaviour { }

    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => null;
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour
    {
        public static EventSystem current { get; }
        public bool IsPointerOverGameObject() => false;
    }
    public class BaseInputModule : Behaviour { }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
    }

    public class Image : Graphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90 }
        public Sprite sprite { get; set; }
        public bool preserveAspect { get; set; }
        public Type type { get; set; }
        public FillMethod fillMethod { get; set; }
        public int fillOrigin { get; set; }
        public float fillAmount { get; set; }
    }

    public class ButtonClickedEvent { public void AddListener(UnityEngine.Events.UnityAction a) { } }

    public struct ColorBlock { public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor; }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public ColorBlock colors { get; set; }
        public Image image { get; set; }
    }

    public class Button : Selectable { public ButtonClickedEvent onClick { get; } }

    public class InputField : Selectable
    {
        public class SubmitEvent { public void AddListener(UnityEngine.Events.UnityAction<string> a) { } }
        public string text { get; set; }
        public Text textComponent { get; set; }
        public SubmitEvent onSubmit { get; }
        public void ActivateInputField() { }
    }

    public class LayoutElement : Behaviour
    {
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float minWidth { get; set; }
        public float minHeight { get; set; }
    }

    public class LayoutGroup : Behaviour { public RectOffset padding { get; set; } public TextAnchor childAlignment { get; set; } }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Vector2 cellSize { get; set; }
        public Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float scaleFactor { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T arg);
}
