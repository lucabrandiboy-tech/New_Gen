// Stand-ins for the UnityEditor APIs the editor-only scripts use. See README.md for limits.
#pragma warning disable CS0649, CS0067, IDE0060
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute { public MenuItem(string path, bool validate = false, int priority = 0) { } }

    public struct GUID
    {
        public static GUID Generate() => default;
    }

    public enum TextureImporterType { Default, Sprite, NormalMap }
    public enum TextureImporterCompression { Uncompressed, Compressed }
    public enum SpriteImportMode { None, Single, Multiple, Polygon }

    public class AssetImporter : UnityEngine.Object { public string assetPath { get; } }

    public class TextureImporterSettings
    {
        public SpriteMeshType spriteMeshType { get; set; }
        public int spriteAlignment { get; set; }
        public uint spriteExtrude { get; set; }
    }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public float spritePixelsPerUnit { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public bool mipmapEnabled { get; set; }
        public bool alphaIsTransparency { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int maxTextureSize { get; set; }
        public bool isReadable { get; set; }
        public SpriteImportMode spriteImportMode { get; set; }
        public void ReadTextureSettings(TextureImporterSettings s) { }
        public void SetTextureSettings(TextureImporterSettings s) { }
    }

    public class AssetPostprocessor
    {
        public string assetPath { get; }
        public AssetImporter assetImporter { get; }
        public virtual uint GetVersion() => 0;
    }

    public static class AssetDatabase
    {
        public static string[] FindAssets(string filter, string[] searchInFolders = null) => new string[0];
        public static string GUIDToAssetPath(string guid) => "";
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => default;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string path) => new UnityEngine.Object[0];
        public static bool IsValidFolder(string path) => false;
        public static string CreateFolder(string parent, string name) => "";
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void SaveAssetIfDirty(UnityEngine.Object o) { }
        public static void Refresh() { }
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object o) { }
    }

    public static class EditorApplication
    {
        public static Action delayCall;
        public static bool isPlaying { get; set; }
    }

    public class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene(string path, bool enabled) { }
        public string path { get; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }

    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => default;
        public static bool SaveScene(Scene scene, string path) => true;
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name { get; } }
}

namespace UnityEngine
{
    public enum SpriteMeshType { FullRect, Tight }
    public enum SpriteAlignment { Center, TopLeft, TopCenter, TopRight, LeftCenter, RightCenter, BottomLeft, BottomCenter, BottomRight, Custom }
}

namespace UnityEditor.U2D.Sprites
{
    using UnityEngine;

    public class SpriteRect
    {
        public string name { get; set; }
        public Rect rect { get; set; }
        public SpriteAlignment alignment { get; set; }
        public Vector2 pivot { get; set; }
        public GUID spriteID { get; set; }
    }

    public struct SpriteNameFileIdPair
    {
        public SpriteNameFileIdPair(string name, GUID id) { }
    }

    public interface ISpriteEditorDataProvider
    {
        void InitSpriteEditorDataProvider();
        SpriteRect[] GetSpriteRects();
        void SetSpriteRects(SpriteRect[] rects);
        void Apply();
        T GetDataProvider<T>() where T : class;
    }

    public interface ISpriteNameFileIdDataProvider
    {
        void SetNameFileIdPairs(IEnumerable<SpriteNameFileIdPair> pairs);
    }

    public class SpriteDataProviderFactories
    {
        public void Init() { }
        public ISpriteEditorDataProvider GetSpriteEditorDataProviderFromObject(UnityEngine.Object obj) => null;
    }
}
