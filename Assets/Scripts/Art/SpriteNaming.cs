using System;
using System.Text.RegularExpressions;

namespace NewGen.Art
{
    /// <summary>
    /// Shared rules for art file names and folders. See Docs/ART_GUIDE.md.
    /// </summary>
    public static class SpriteNaming
    {
        /// <summary>Owner's hand-drawn art. Claude never writes here.</summary>
        public const string UserFolder = "Assets/Art/Sprites";

        /// <summary>Generated flat-colour placeholders. Safe to regenerate.</summary>
        public const string PlaceholderFolder = "Assets/Art/_Placeholders";

        /// <summary>Finished art made by Claude (terrain, roads). Beats placeholders, loses to the owner's art.</summary>
        public const string GeneratedFolder = "Assets/Art/Generated";

        public const int PixelsPerUnit = 64;

        // "unit_player_walk_4f" -> 4 frames. Must be the very end of the name.
        static readonly Regex FrameSuffix = new Regex(@"_(\d+)f$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsInUserFolder(string assetPath) => IsInFolder(assetPath, UserFolder);
        public static bool IsInPlaceholderFolder(string assetPath) => IsInFolder(assetPath, PlaceholderFolder);
        public static bool IsInGeneratedFolder(string assetPath) => IsInFolder(assetPath, GeneratedFolder);
        public static bool IsInArtFolder(string assetPath) =>
            IsInUserFolder(assetPath) || IsInGeneratedFolder(assetPath) || IsInPlaceholderFolder(assetPath);

        static bool IsInFolder(string assetPath, string folder)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            string p = assetPath.Replace('\\', '/');
            return p.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Number of animation frames encoded in the file name (1 if none).</summary>
        public static int GetFrameCount(string fileNameNoExt)
        {
            if (string.IsNullOrEmpty(fileNameNoExt)) return 1;
            Match m = FrameSuffix.Match(fileNameNoExt);
            if (!m.Success) return 1;
            if (!int.TryParse(m.Groups[1].Value, out int n) || n < 1) return 1;
            return n;
        }

        /// <summary>
        /// The name game code uses to ask for a sprite: lowercase, frame suffix removed.
        /// "Unit_Player_Walk_4f" -> "unit_player_walk".
        /// </summary>
        public static string ToKey(string fileNameNoExt)
        {
            if (string.IsNullOrEmpty(fileNameNoExt)) return string.Empty;
            Match m = FrameSuffix.Match(fileNameNoExt);
            // Only strip a suffix that GetFrameCount would also accept (e.g. not "_0f").
            bool validSuffix = m.Success && int.TryParse(m.Groups[1].Value, out int n) && n >= 1;
            string baseName = validSuffix ? fileNameNoExt.Substring(0, m.Index) : fileNameNoExt;
            return baseName.ToLowerInvariant();
        }

        /// <summary>Buildings are anchored at the bottom-left of their footprint; everything else at the centre.</summary>
        public static bool UsesBottomLeftPivot(string fileNameNoExt)
        {
            return fileNameNoExt != null && fileNameNoExt.StartsWith("bld_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True if the name follows the convention (lowercase letters, digits, underscores).</summary>
        public static bool IsValidName(string fileNameNoExt)
        {
            return !string.IsNullOrEmpty(fileNameNoExt) && Regex.IsMatch(fileNameNoExt, "^[a-z0-9_]+$");
        }
    }
}
