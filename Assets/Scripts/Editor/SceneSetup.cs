using NewGen.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NewGen.EditorTools
{
    /// <summary>Creates Assets/Scenes/Game.unity with the one object the game needs (GameRoot builds the rest at runtime).</summary>
    public static class SceneSetup
    {
        const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("New Generation/Create Game Scene")]
        public static void CreateGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 10f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            var root = new GameObject("Game");
            root.AddComponent<GameRoot>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            // make it the first scene in builds
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Scene] Created " + ScenePath + ". Press Play.");
        }
    }
}
