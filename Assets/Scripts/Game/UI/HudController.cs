using System.Collections.Generic;
using NewGen.Art;
using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NewGen.Game.UI
{
    /// <summary>
    /// Placeholder HUD + menus, all built from code so the owner can reskin later.
    /// Layout: bars top-left, alerts top-right, quickbar bottom-centre, research summary bottom-right.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        GameRoot root;
        Canvas canvas; CanvasScaler scaler;
        GameObject hud, menuRoot, pauseRoot, deathRoot;
        Image healthFill, tempFill; Text healthLabel, tempLabel, powerText, researchText, saveText, toastText, hintText;
        float toastUntil;
        Transform alertsList; readonly Dictionary<string, (Text text, float until)> alerts = new Dictionary<string, (Text, float)>();
        readonly Button[] quickButtons = new Button[10]; readonly Image[] quickIcons = new Image[10]; readonly Text[] quickLabels = new Text[10];
        readonly List<string> quickItems = new List<string>(); int quickPage;
        public MainMenuPanel Menu { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public ResearchPanel Research { get; private set; }
        public InspectPanel Inspector { get; private set; }
        public SettingsPanel Settings { get; private set; }
        public Transform Overlay { get; private set; }
        public bool PointerOverUi => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public static HudController Create(GameRoot root)
        {
            UiKit.EnsureFont();
            var go = new GameObject("HUD");
            var h = go.AddComponent<HudController>();
            h.root = root;
            h.BuildCanvas();
            return h;
        }

        void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.scaleFactor = PlayerPrefs.GetFloat("ng.uiscale", 1f);
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            hud = new GameObject("Game HUD", typeof(RectTransform)); hud.transform.SetParent(transform, false); UiKit.Stretch(hud);
            BuildBars(); BuildQuickbar(); BuildAlerts(); BuildBottomRight();
            Overlay = new GameObject("Overlay", typeof(RectTransform)).transform; Overlay.SetParent(transform, false); UiKit.Stretch(Overlay.gameObject);
            Inventory = InventoryPanel.Create(root, Overlay);
            Research = ResearchPanel.Create(root, Overlay);
            Inspector = InspectPanel.Create(root, Overlay);
            Settings = SettingsPanel.Create(root, Overlay, this);
            BuildPause(); BuildDeath();
            Menu = MainMenuPanel.Create(root, transform, this);
            toastText = UiKit.Text_(transform, "", 24, TextAnchor.MiddleCenter, UiKit.Accent);
            UiKit.Place(toastText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 40f));
            toastText.raycastTarget = false;
        }

        public void SetUiScale(float s) { scaler.scaleFactor = Mathf.Clamp(s, 0.6f, 1.8f); PlayerPrefs.SetFloat("ng.uiscale", scaler.scaleFactor); }

        // ---------------------------------------------------------------- HUD pieces
        void BuildBars()
        {
            var box = UiKit.Panel_(hud.transform, "Bars", UiKit.Panel);
            UiKit.Place(box, new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(320f, 120f));
            UiKit.VList(box, 4, 8);
            healthFill = UiKit.Bar(box.transform, new Vector2(300f, 26f), UiKit.Danger, out healthLabel);
            tempFill = UiKit.Bar(box.transform, new Vector2(300f, 26f), new Color(0.35f, 0.7f, 1f), out tempLabel);
            powerText = UiKit.Text_(box.transform, "", 16, TextAnchor.MiddleLeft, UiKit.Dim);
            hintText = UiKit.Text_(hud.transform, "LMB shoot/build   RMB mine/remove   R rotate   Q cancel   E inventory   T research   F5 save   Esc menu", 14, TextAnchor.MiddleCenter, UiKit.Dim);
            UiKit.Place(hintText.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(1400f, 20f));
        }

        void BuildQuickbar()
        {
            var bar = UiKit.Panel_(hud.transform, "Quickbar", UiKit.Panel);
            UiKit.Place(bar, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(10 * 76f + 16f, 84f));
            var h = UiKit.HList(bar, 4, 8); h.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 10; i++)
            {
                int idx = i;
                var b = UiKit.Button_(bar.transform, "", () => PressQuickbar(idx), new Vector2(72f, 72f));
                var icon = UiKit.Image_(b.transform, null, new Vector2(56f, 56f));
                UiKit.Place(icon.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(56f, 56f));
                var lbl = b.GetComponentInChildren<Text>(); lbl.text = ((i + 1) % 10).ToString(); lbl.alignment = TextAnchor.LowerLeft; lbl.fontSize = 14; lbl.color = UiKit.Dim;
                quickButtons[i] = b; quickIcons[i] = icon; quickLabels[i] = lbl;
            }
        }

        void BuildAlerts()
        {
            var box = new GameObject("Alerts", typeof(RectTransform)); box.transform.SetParent(hud.transform, false);
            UiKit.Place(box, new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(360f, 300f));
            UiKit.VList(box, 4, 0);
            alertsList = box.transform;
        }

        void BuildBottomRight()
        {
            var box = UiKit.Panel_(hud.transform, "Status", UiKit.Panel);
            UiKit.Place(box, new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(360f, 90f));
            UiKit.VList(box, 2, 8);
            researchText = UiKit.Text_(box.transform, "", 16, TextAnchor.MiddleLeft);
            saveText = UiKit.Text_(box.transform, "", 16, TextAnchor.MiddleLeft, UiKit.Dim);
        }

        void BuildPause()
        {
            pauseRoot = UiKit.Panel_(transform, "Pause", new Color(0f, 0f, 0f, 0.6f)); UiKit.Stretch(pauseRoot);
            var box = UiKit.Panel_(pauseRoot.transform, "Box", UiKit.Panel);
            UiKit.Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 330f));
            UiKit.VList(box, 8, 16);
            UiKit.Text_(box.transform, "PAUSED", 28, TextAnchor.MiddleCenter);
            UiKit.Button_(box.transform, "Resume", () => SetPause(false));
            UiKit.Button_(box.transform, "Save (F5)", () => TrySave());
            UiKit.Button_(box.transform, "Settings", () => Settings.Toggle());
            UiKit.Button_(box.transform, "Main menu", () => { SetPause(false); root.ToMenu(); });
            UiKit.Button_(box.transform, "Quit game", () => root.QuitGame());
            pauseRoot.SetActive(false);
        }

        void BuildDeath()
        {
            deathRoot = UiKit.Panel_(transform, "Death", new Color(0.3f, 0f, 0f, 0.75f)); UiKit.Stretch(deathRoot);
            var box = UiKit.Panel_(deathRoot.transform, "Box", UiKit.Panel);
            UiKit.Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 220f));
            UiKit.VList(box, 10, 16);
            var t = UiKit.Text_(box.transform, "", 24, TextAnchor.MiddleCenter, UiKit.Danger); t.name = "DeathText";
            UiKit.Button_(box.transform, "Reload last save", () => { deathRoot.SetActive(false); root.ReloadLastSave(); });
            UiKit.Button_(box.transform, "Main menu", () => { deathRoot.SetActive(false); root.ToMenu(); });
            deathRoot.SetActive(false);
        }

        // ---------------------------------------------------------------- modes
        public void ShowMainMenu()
        {
            hud.SetActive(false); pauseRoot.SetActive(false); deathRoot.SetActive(false);
            Inventory.Hide(); Research.Hide(); Inspector.Hide(); Settings.Hide();
            Menu.Show();
        }

        public void ShowGame()
        {
            Menu.Hide(); hud.SetActive(true); pauseRoot.SetActive(false); deathRoot.SetActive(false);
            Inventory.Hide(); Research.Hide(); Inspector.Hide(); Settings.Hide();
            RebuildQuickbar();
        }

        public void ShowDeath(string text)
        {
            deathRoot.SetActive(true);
            deathRoot.transform.Find("Box/DeathText").GetComponent<Text>().text = text;
        }

        public void SetPause(bool p) { pauseRoot.SetActive(p); root.SetPaused(p); if (!p) Settings.Hide(); }

        public void Toast(string msg) { toastText.text = msg; toastUntil = Time.unscaledTime + 2.5f; }

        public void TrySave()
        {
            if (root.ManualSave(out int left)) return;
            Toast(string.Format(root.Db.Text("alert.save_cooldown", "You can save again in {0}"), UiKit.Seconds(left)));
        }

        // ---------------------------------------------------------------- quickbar (unlocked buildings)
        public void RebuildQuickbar()
        {
            quickItems.Clear();
            if (root.Sim == null) return;
            foreach (var b in root.Db.Buildings.Values)
            {
                if (b.Id == "crashpod") continue;
                if (b.UnlockedAtStart || root.Sim.UnlockedBuildings.Contains(b.Id)) quickItems.Add(b.Id);
            }
            quickItems.Sort();
            RefreshQuickbar();
        }

        void RefreshQuickbar()
        {
            for (int i = 0; i < 10; i++)
            {
                int idx = quickPage * 10 + i;
                string id = idx < quickItems.Count ? quickItems[idx] : null;
                var def = id == null ? null : root.Db.Building(id);
                quickIcons[i].sprite = def == null ? null : (def.IsConveyor ? SpriteRegistry.Get("bld_" + id + "_straight") : SpriteRegistry.Get(def.Kind == BuildingKind.Arm ? "bld_arm_side_idle" : "bld_" + id + "_idle"));
                quickIcons[i].enabled = def != null;
                quickButtons[i].image.color = id != null && id == root.Build.Selected ? UiKit.Accent : UiKit.PanelLight;
            }
        }

        public void PressQuickbar(int i)
        {
            int idx = quickPage * 10 + i;
            if (idx >= quickItems.Count) return;
            string id = quickItems[idx];
            root.Build.Select(root.Build.Selected == id ? null : id);
        }

        public void QuickbarPage(int delta)
        {
            int pages = Mathf.Max(1, (quickItems.Count + 9) / 10);
            quickPage = (quickPage + delta + pages) % pages;
            RefreshQuickbar();
            Toast("Quickbar page " + (quickPage + 1) + "/" + pages);
        }

        public void OnSelectionChanged() => RefreshQuickbar();

        // ---------------------------------------------------------------- panels
        public void ToggleInventory() { if (Inventory.Visible) Inventory.Hide(); else { Research.Hide(); Inventory.Show(); } }
        public void ToggleResearch() { if (Research.Visible) Research.Hide(); else { Inventory.Hide(); Research.Show(); } }
        public void Inspect(Building b) => Inspector.Show(b);
        public void CloseInspect() => Inspector.Hide();
        public void CloseInspectIf(Building b) { if (Inspector.Target == b) Inspector.Hide(); }

        // ---------------------------------------------------------------- events
        public void HandleEvent(SimEvent e)
        {
            switch (e.Type)
            {
                case SimEventType.Alert: Alert(e.Text, root.Db.Text(e.Text, e.Text)); break;
                case SimEventType.ResearchFinished:
                    Toast("Research complete: " + (root.Db.Tech(e.Text)?.Name ?? e.Text));
                    RebuildQuickbar(); Research.Refresh(); break;
                case SimEventType.ItemPickedUp: if (Inventory.Visible) Inventory.Refresh(); break;
            }
        }

        void Alert(string key, string text)
        {
            if (!alerts.TryGetValue(key, out var a) || a.text == null)
            {
                var row = UiKit.Panel_(alertsList, "Alert", UiKit.Panel);
                var le = row.AddComponent<LayoutElement>(); le.preferredHeight = 34f;
                var t = UiKit.Text_(row.transform, text, 18, TextAnchor.MiddleLeft, UiKit.Danger);
                UiKit.Stretch(t.gameObject, 8, 8, 2, 2);
                a = (t, 0f);
            }
            a.until = Time.unscaledTime + 4f;
            alerts[key] = a;
        }

        void Update()
        {
            if (toastUntil > 0f && Time.unscaledTime > toastUntil) { toastText.text = ""; toastUntil = 0f; }
            // expire alerts
            List<string> dead = null;
            foreach (var kv in alerts)
                if (Time.unscaledTime > kv.Value.until) { if (kv.Value.text != null) Destroy(kv.Value.text.transform.parent.gameObject); (dead ??= new List<string>()).Add(kv.Key); }
            if (dead != null) foreach (var k in dead) alerts.Remove(k);

            if (root.Sim == null || !hud.activeSelf) return;
            if (root.Input.Pause.WasPressedThisFrame() && !root.Console.IsOpen)
            {
                if (Inventory.Visible || Research.Visible || Inspector.Visible || Settings.Visible) { Inventory.Hide(); Research.Hide(); Inspector.Hide(); Settings.Hide(); }
                else if (root.Mode == GameMode.Playing || root.Mode == GameMode.Paused) SetPause(root.Mode != GameMode.Paused);
            }
            var p = root.Sim.LocalPlayer;
            if (p != null)
            {
                healthFill.fillAmount = p.Health / (float)root.Sim.Balance.PlayerMaxHealth;
                healthLabel.text = root.Db.Text("ui.health", "Health") + " " + p.Health;
                tempFill.fillAmount = p.Temperature / (float)root.Sim.Balance.PlayerMaxTemperature;
                tempLabel.text = root.Db.Text("ui.temperature", "Body temperature") + " " + (p.Temperature / 10) + "%";
                tempFill.color = p.Temperature < 250 ? UiKit.Danger : new Color(0.35f, 0.7f, 1f);
            }
            // power summary: biggest network
            long supply = 0, demand = 0; int sat = 1000;
            foreach (var n in root.Sim.Networks.Values) if (n.Demand + n.Supply > supply + demand) { supply = n.Supply; demand = n.Demand; sat = n.Satisfaction; }
            powerText.text = demand == 0 && supply == 0 ? "No power network" : $"Power {UiKit.Watts(supply)} / {UiKit.Watts(demand)} ({sat / 10}%)";
            // research + save
            var cur = root.Sim.Research.Current;
            researchText.text = cur == null ? root.Db.Text("ui.research", "Research") + ": nothing queued (T)" : $"{cur.Name}: {root.Sim.Research.ProgressOfCurrent1000() / 10}%  (+{root.Sim.Research.Queue.Count - 1} queued)";
            bool can = Sim.Save.SaveSystem.CanManualSave(root.Sim, out int left);
            saveText.text = (root.Sim.Settings.Ironman ? "IRONMAN  " : "") + (can ? "Save ready (F5)" : "Next save in " + UiKit.Seconds(left));
            if (Inspector.Visible) Inspector.Refresh();
            if (Inventory.Visible && p != null) Inventory.RefreshIfChanged();
        }
    }
}
