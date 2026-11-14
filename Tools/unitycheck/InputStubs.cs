// Stand-ins for the Unity Input System APIs this project uses. See UnityStubs.cs for what this does and does not catch.
#pragma warning disable CS0649, CS0067, IDE0060
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }

    public class InputAction
    {
        public string name { get; }
        public void Enable() { }
        public void Disable() { }
        public T ReadValue<T>() => default;
        public bool IsPressed() => false;
        public bool WasPressedThisFrame() => false;
        public bool WasReleasedThisFrame() => false;
    }

    public class InputActionMap : IDisposable
    {
        public InputActionMap(string name) { }
        public IEnumerable<InputAction> actions { get; }
        public void Enable() { }
        public void Disable() { }
        public void Dispose() { }
    }

    public static class InputActionSetupExtensions
    {
        public static InputAction AddAction(this InputActionMap map, string name, InputActionType type = InputActionType.Value,
            string binding = null, string interactions = null, string processors = null, string groups = null, string expectedControlType = null) => new InputAction();

        public static CompositeSyntax AddCompositeBinding(this InputAction action, string composite, string interactions = null, string processors = null) => default;

        public struct CompositeSyntax
        {
            public CompositeSyntax With(string name, string binding, string groups = null, string processors = null) => this;
        }
    }

    public static class InputActionRebindingExtensions
    {
        public static RebindingOperation PerformInteractiveRebinding(this InputAction action, int bindingIndex = -1) => new RebindingOperation();
        public static string GetBindingDisplayString(this InputAction action) => "";
        public static string SaveBindingOverridesAsJson(this InputActionMap map) => "";
        public static void LoadBindingOverridesFromJson(this InputActionMap map, string json, bool removeExisting = true) { }
        public static void RemoveAllBindingOverrides(this InputActionMap map) { }

        public class RebindingOperation : IDisposable
        {
            public RebindingOperation WithControlsExcluding(string path) => this;
            public RebindingOperation WithCancelingThrough(string path) => this;
            public RebindingOperation OnComplete(Action<RebindingOperation> cb) => this;
            public RebindingOperation OnCancel(Action<RebindingOperation> cb) => this;
            public RebindingOperation Start() => this;
            public void Dispose() { }
        }
    }

    public class Vector2Control { public Vector2 ReadValue() => default; }

    public class Mouse
    {
        public static Mouse current { get; }
        public Vector2Control position { get; }
    }

    public class Keyboard { public static Keyboard current { get; } }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { }
}
