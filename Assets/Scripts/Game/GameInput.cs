using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NewGen.Game
{
    /// <summary>
    /// All input actions, defined in code (no asset to maintain). Keyboard + mouse only for now.
    /// Bindings can be changed at runtime and are saved as JSON in PlayerPrefs ("ng.bindings").
    /// </summary>
    public sealed class GameInput : IDisposable
    {
        public readonly InputActionMap Map = new InputActionMap("Game");
        public readonly InputAction Move, Fire, Mine, Rotate, Cancel, Inventory, Pause, DevConsole, Research, Build, Interact, Zoom;
        public readonly InputAction[] Quickbar = new InputAction[10];
        public readonly InputAction QuickbarNext, QuickbarPrev, QuickSave;

        const string PrefsKey = "ng.bindings";

        public GameInput()
        {
            Move = Map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            Fire = Map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            Build = Map.AddAction("Build", InputActionType.Button, "<Mouse>/leftButton");      // same button: context decides
            Mine = Map.AddAction("Mine", InputActionType.Button, "<Mouse>/rightButton");
            Interact = Map.AddAction("Interact", InputActionType.Button, "<Keyboard>/f");
            Rotate = Map.AddAction("Rotate", InputActionType.Button, "<Keyboard>/r");
            Cancel = Map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/q");
            Inventory = Map.AddAction("Inventory", InputActionType.Button, "<Keyboard>/e");
            Research = Map.AddAction("Research", InputActionType.Button, "<Keyboard>/t");
            Pause = Map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            DevConsole = Map.AddAction("DevConsole", InputActionType.Button, "<Keyboard>/backquote");
            QuickSave = Map.AddAction("QuickSave", InputActionType.Button, "<Keyboard>/f5");
            Zoom = Map.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");
            QuickbarNext = Map.AddAction("QuickbarNext", InputActionType.Button, "<Keyboard>/x");
            QuickbarPrev = Map.AddAction("QuickbarPrev", InputActionType.Button, "<Keyboard>/z");
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            for (int i = 0; i < 10; i++) Quickbar[i] = Map.AddAction("Quickbar" + (i + 1), InputActionType.Button, "<Keyboard>/" + keys[i]);

            LoadOverrides();
            Map.Enable();
        }

        public Vector2 MoveValue => Move.ReadValue<Vector2>();
        public Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public float ZoomDelta => Zoom.ReadValue<float>();

        public IEnumerable<InputAction> RebindableActions()
        {
            foreach (var a in Map.actions) if (a != Move && a != Zoom && a != Build) yield return a;
        }

        /// <summary>Start listening for a new key for an action. onDone(true) when rebound, false if cancelled (Escape).</summary>
        public void Rebind(InputAction action, Action<bool> onDone)
        {
            action.Disable();
            var op = action.PerformInteractiveRebinding(0)
                .WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(o => { o.Dispose(); action.Enable(); SaveOverrides(); onDone?.Invoke(true); })
                .OnCancel(o => { o.Dispose(); action.Enable(); onDone?.Invoke(false); });
            op.Start();
        }

        public void ResetBindings()
        {
            Map.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PrefsKey);
        }

        public string BindingDisplay(InputAction a) => a.GetBindingDisplayString();

        void SaveOverrides() { PlayerPrefs.SetString(PrefsKey, Map.SaveBindingOverridesAsJson()); PlayerPrefs.Save(); }
        void LoadOverrides()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json)) { try { Map.LoadBindingOverridesFromJson(json); } catch (Exception e) { Debug.LogWarning("[Input] bad saved bindings: " + e.Message); } }
        }

        public void Dispose() { Map.Disable(); Map.Dispose(); }
    }
}
