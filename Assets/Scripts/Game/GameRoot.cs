using System;
using System.IO;
using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using NewGen.Sim.Save;
using NewGen.Game.UI;
using UnityEngine;

namespace NewGen.Game
{
    public enum GameMode { Menu, Playing, Paused, Dead }

    /// <summary>
    /// Owns the simulation and everything that renders it. Put this on one empty GameObject in a scene
    /// (menu: New Generation > Create Game Scene). It creates the camera, UI, renderers and input itself.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public ContentDatabase Db { get; private set; }
        public Simulation Sim { get; private set; }
        public GameInput Input { get; private set; }
        public CameraRig Camera { get; private set; }
        public TerrainRenderer Terrain { get; private set; }
        public EntityViewManager Views { get; private set; }
        public BuildTool Build { get; private set; }
        public HudController Hud { get; private set; }
        public DevConsole Console { get; private set; }
        public GameMode Mode { get; private set; } = GameMode.Menu;
        public int CurrentSlot { get; private set; } = 1;
        public const int SlotCount = 5;

        float accumulator;
        public float TickInterval => 1f / (Db?.Balance.TicksPerSecond ?? 30);
        /// <summary>0..1 progress between the last tick and the next (for smoothing).</summary>
        public float TickAlpha => Mathf.Clamp01(accumulator / TickInterval);
        public event Action<SimEvent> OnSimEvent;

        public string SaveDir => Path.Combine(Application.persistentDataPath, "saves");
        public string SlotPath(int slot) => Path.Combine(SaveDir, "slot" + slot + ".ngs");

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SimLog.Info = s => Debug.Log(s);
            SimLog.Warn = s => Debug.LogWarning(s);
            SimLog.Error = s => Debug.LogError(s);

            Db = ContentDatabase.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, "Content"));
            Input = new GameInput();
            Camera = CameraRig.Create();
            EnsureGlobalLight();
            Terrain = TerrainRenderer.Create(this);
            Views = EntityViewManager.Create(this);
            Build = BuildTool.Create(this);
            Hud = HudController.Create(this);
            Console = DevConsole.Create(this);
            Hud.ShowMainMenu();
            Debug.Log($"[Game] content loaded: {Db.Items.Count} items, {Db.Buildings.Count} buildings, {Db.Techs.Count} techs, {Db.Problems.Count} problems");
        }

        void OnDestroy() { Input?.Dispose(); if (Instance == this) Instance = null; }

        static void EnsureGlobalLight()
        {
            foreach (var l in FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None))
                if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) return;
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            light.intensity = 0.85f;
            light.color = new Color(0.8f, 0.86f, 1f);
        }

        // ------------------------------------------------------------ game flow
        public void NewGame(int slot, bool ironman, string difficulty, ulong seed)
        {
            CurrentSlot = slot;
            AttachSim(new Simulation(Db, seed));
            Sim.Settings.Ironman = ironman; Sim.Settings.Difficulty = difficulty;
            Sim.StartNewGame();
            Views.Rebuild();
            Mode = GameMode.Playing;
            Hud.ShowGame();
        }

        public bool LoadGame(int slot)
        {
            string path = SlotPath(slot);
            if (!File.Exists(path)) return false;
            try
            {
                var sim = SaveSystem.Read(File.ReadAllBytes(path), Db);
                CurrentSlot = slot;
                AttachSim(sim);
                Views.Rebuild();
                Mode = GameMode.Playing;
                Hud.ShowGame();
                return true;
            }
            catch (Exception e) { Debug.LogError("[Save] load failed: " + e.Message); return false; }
        }

        public bool SlotExists(int slot) => File.Exists(SlotPath(slot));
        public DateTime SlotTime(int slot) => File.GetLastWriteTime(SlotPath(slot));

        /// <summary>Manual save respecting the cooldown. Returns false (and the seconds left) if not allowed.</summary>
        public bool ManualSave(out int secondsLeft)
        {
            secondsLeft = 0;
            if (Sim == null) return false;
            if (!SaveSystem.CanManualSave(Sim, out secondsLeft)) return false;
            WriteSave(true);
            return true;
        }

        public void AutosaveIfAllowed()
        {
            if (Sim == null || Mode == GameMode.Menu) return;
            if (!SaveSystem.AutosaveAllowed(Sim)) return;
            WriteSave(false);
        }

        void WriteSave(bool manual)
        {
            Directory.CreateDirectory(SaveDir);
            File.WriteAllBytes(SlotPath(CurrentSlot), SaveSystem.Write(Sim, manual));
            Hud.Toast(manual ? "Saved" : "Autosaved");
        }

        public void ReloadLastSave()
        {
            if (!LoadGame(CurrentSlot)) ToMenu();
        }

        public void ToMenu()
        {
            AutosaveIfAllowed();
            Mode = GameMode.Menu;
            Views.Clear(); Terrain.Clear();
            Sim = null;
            Hud.ShowMainMenu();
        }

        public void QuitGame()
        {
            AutosaveIfAllowed();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetPaused(bool paused)
        {
            if (Sim == null) return;
            Mode = paused ? GameMode.Paused : GameMode.Playing;
        }

        void AttachSim(Simulation sim)
        {
            Sim = sim; accumulator = 0f;
            Terrain.Clear(); Views.Clear();
        }

        void OnApplicationQuit() => AutosaveIfAllowed();

        // ------------------------------------------------------------ loop
        void Update()
        {
            if (Sim == null) return;
            if (Mode == GameMode.Playing)
            {
                Build.UpdateInput();
                accumulator += Time.deltaTime;
                int safety = 0;
                while (accumulator >= TickInterval && safety++ < 5)
                {
                    accumulator -= TickInterval;
                    ApplyPlayerInput();
                    Sim.Step();
                }
                if (safety >= 5) accumulator = 0f;   // dropped frames: do not spiral
                DrainEvents();
            }
            Terrain.Refresh();
            Views.Refresh();
        }

        void ApplyPlayerInput()
        {
            var p = Sim.LocalPlayer;
            if (p == null || p.Dead) return;
            bool uiBlocks = Hud.PointerOverUi;
            var mv = Input.MoveValue;
            var aim = WorldSpace.ToSub(Camera.MouseWorld(Input.MousePosition));
            p.Input = new PlayerInput
            {
                MoveX = Mathf.RoundToInt(Mathf.Clamp(mv.x, -1f, 1f) * 1000f),
                MoveY = Mathf.RoundToInt(Mathf.Clamp(mv.y, -1f, 1f) * 1000f),
                Aim = aim,
                Fire = !uiBlocks && !Build.HasSelection && Input.Fire.IsPressed() && !Console.IsOpen,
                Mine = !uiBlocks && Build.MineHeld,
            };
        }

        void DrainEvents()
        {
            foreach (var e in Sim.Events)
            {
                if (e.Type == SimEventType.PlayerDied) { Mode = GameMode.Dead; Hud.ShowDeath(Db.Text(e.Text, "You died.")); }
                Views.HandleEvent(e);
                Hud.HandleEvent(e);
                OnSimEvent?.Invoke(e);
            }
            Sim.Events.Clear();
        }
    }
}
