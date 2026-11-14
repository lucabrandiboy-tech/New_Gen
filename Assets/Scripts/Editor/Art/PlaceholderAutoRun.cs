using System.IO;
using NewGen.Art;
using UnityEditor;
using UnityEngine;

namespace NewGen.EditorTools.Art
{
    /// <summary>
    /// Generates the placeholder sprites automatically when some are missing, so the game is always
    /// playable without anyone remembering to use the menu. Runs at most once per editor session.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaceholderAutoRun
    {
        static PlaceholderAutoRun()
        {
            EditorApplication.delayCall += RunOnce;
        }

        static void RunOnce()
        {
            try
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), SpriteNaming.PlaceholderFolder);
                int onDisk = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.png").Length : 0;
                int expected = SpriteManifest.All.Count;
                if (onDisk >= expected) return;
                Debug.Log($"[Art] {onDisk}/{expected} placeholder sprites present — generating the missing ones.");
                PlaceholderGenerator.Generate();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Art] automatic placeholder generation failed: " + e.Message +
                                 "\nUse New Generation > Art > Generate Placeholders by hand.");
            }
        }
    }
}
