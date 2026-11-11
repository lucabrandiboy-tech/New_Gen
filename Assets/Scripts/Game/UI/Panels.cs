using System;
using System.Collections.Generic;
using NewGen.Art;
using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace NewGen.Game.UI
{
    public abstract class UiPanel : MonoBehaviour
    {
        protected GameRoot Root;
        protected GameObject Box;
        public bool Visible => Box != null && Box.activeSelf;
        public virtual void Show() { Box.SetActive(true); Refresh(); }
        public virtual void Hide() { if (Box != null) Box.SetActive(false); }
        public void Toggle() { if (Visible) Hide(); else Show(); }
        public abstract void Refresh();

        protected static T Make<T>(GameRoot root, Transform parent, string title, Vector2 anchor, Vector2 pos, Vector2 size) where T : UiPanel
        {
            var go = new GameObject(title, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var p = go.AddComponent<T>(); p.Root = root;
            p.Box = UiKit.Panel_(go.transform, "Box", UiKit.Panel);
            UiKit.Place(p.Box, anchor, pos, size);
            UiKit.VList(p.Box, 6, 12);
            var head = UiKit.Row(p.Box.transform, 36f);
            UiKit.Text_(head.transform, title, 22, TextAnchor.MiddleLeft, UiKit.Accent);
            UiKit.Button_(head.transform, "X", p.Hide, new Vector2(36f, 32f));
            p.Box.SetActive(false);
            return p;
        }

        protected static GameObject Section(Transform parent, string title)
        {
            var go = new GameObject("Section " + title, typeof(RectTransform)); go.transform.SetParent(parent, false);
            UiKit.VList(go, 4, 0);
            if (!string.IsNullOrEmpty(title)) UiKit.Text_(go.transform, title, 18, TextAnchor.MiddleLeft, UiKit.Dim);
            return go;
        }

        protected static GameObject SlotGrid(Transform parent, int columns, float cell = 56f)
        {
            var go = new GameObject("Grid", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var g = go.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(cell, cell); g.spacing = new Vector2(4f, 4f); g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = columns;
            return go;
        }

        protected GameObject Slot(Transform grid, ItemStack stack, Action onClick = null)
        {
            var cell = UiKit.Panel_(grid, "Slot", UiKit.PanelLight);
            if (onClick != null) cell.AddComponent<Button>().onClick.AddListener(() => onClick());
            if (stack != null && stack.Count > 0)
            {
                var def = Root.Db.Item(stack.Item);
                var img = UiKit.Image_(cell.transform, SpriteRegistry.Get(def != null ? def.SpriteKey : "item_" + stack.Item), new Vector2(44f, 44f));
                UiKit.Place(img.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(44f, 44f));
                var t = UiKit.Text_(cell.transform, stack.Count.ToString(), 14, TextAnchor.LowerRight);
                UiKit.Stretch(t.gameObject, 2, 3, 2, 1);
            }
            return cell;
        }
    }

    // =================================================================================================
    public sealed class InventoryPanel : UiPanel
    {
        Transform grid, craftList, queueList; int shownVersion = -1;

        public static InventoryPanel Create(GameRoot root, Transform parent)
        {
            var p = Make<InventoryPanel>(root, parent, "Inventory (E)", new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(560f, 720f));
            p.grid = SlotGrid(p.Box.transform, 8).transform;
            var craft = Section(p.Box.transform, "Hand crafting (no recipes yet)");
            p.craftList = craft.transform;
            var queue = Section(p.Box.transform, "Crafting queue");
            p.queueList = queue.transform;
            return p;
        }

        public void RefreshIfChanged() { var pl = Root.Sim?.LocalPlayer; if (pl != null && pl.Version != shownVersion) Refresh(); }

        public override void Refresh()
        {
            var pl = Root.Sim?.LocalPlayer; if (pl == null) return;
            shownVersion = pl.Version;
            UiKit.Clear(grid);
            foreach (var s in pl.Inventory.Slots) Slot(grid, s);
            // hand recipes
            for (int i = craftList.childCount - 1; i >= 1; i--) Destroy(craftList.GetChild(i).gameObject);
            foreach (var r in Root.Sim.AvailableRecipes("hand"))
            {
                var row = UiKit.Row(craftList, 40f);
                string ins = ""; foreach (var s in r.Inputs) ins += s.Count + " " + (Root.Db.Item(s.Item)?.Name ?? s.Item) + "  ";
                UiKit.Text_(row.transform, $"{r.Name}  ({ins.Trim()})", 16, TextAnchor.MiddleLeft);
                bool can = Root.Sim.CanHandCraft(pl, r.Id, out _);
                var b = UiKit.Button_(row.transform, "Craft", () => { Root.Sim.QueueHandCraft(pl, r.Id, 1); Refresh(); }, new Vector2(90f, 32f));
                b.interactable = can;
                var b5 = UiKit.Button_(row.transform, "x5", () => { Root.Sim.QueueHandCraft(pl, r.Id, 5); Refresh(); }, new Vector2(50f, 32f));
                b5.interactable = can;
            }
            for (int i = queueList.childCount - 1; i >= 1; i--) Destroy(queueList.GetChild(i).gameObject);
            for (int i = 0; i < pl.CraftQueue.Count; i++)
            {
                int idx = i; var job = pl.CraftQueue[i]; var r = Root.Db.Recipe(job.RecipeId);
                var row = UiKit.Row(queueList, 32f);
                int pct = r == null || r.Ticks == 0 ? 0 : job.Progress / (r.Ticks * 10);
                UiKit.Text_(row.transform, $"{r?.Name ?? job.RecipeId}  {pct}%", 16, TextAnchor.MiddleLeft);
                UiKit.Button_(row.transform, "Cancel", () => { Root.Sim.CancelHandCraft(pl, idx); Refresh(); }, new Vector2(80f, 28f));
            }
        }
    }

    // =================================================================================================
    public sealed class ResearchPanel : UiPanel
    {
        Transform list, queue; int shownVersion = -1;

        public static ResearchPanel Create(GameRoot root, Transform parent)
        {
            var p = Make<ResearchPanel>(root, parent, "Research (T)", new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(620f, 720f));
            p.queue = Section(p.Box.transform, "Queue (max 10)").transform;
            p.list = Section(p.Box.transform, "Available").transform;
            return p;
        }

        public override void Refresh()
        {
            if (Root.Sim == null) return;
            var rs = Root.Sim.Research; shownVersion = rs.Version;
            for (int i = queue.childCount - 1; i >= 1; i--) Destroy(queue.GetChild(i).gameObject);
            for (int i = 0; i < rs.Queue.Count; i++)
            {
                var t = Root.Db.Tech(rs.Queue[i]); string id = rs.Queue[i];
                var row = UiKit.Row(queue, 36f);
                string prog = i == 0 ? $"  {rs.ProgressOfCurrent1000() / 10}%" : "";
                UiKit.Text_(row.transform, $"{i + 1}. {t?.Name ?? id}{prog}", 16, TextAnchor.MiddleLeft);
                UiKit.Button_(row.transform, "Remove", () => { rs.Dequeue(id); Refresh(); }, new Vector2(90f, 28f));
            }
            for (int i = list.childCount - 1; i >= 1; i--) Destroy(list.GetChild(i).gameObject);
            foreach (var t in Root.Db.Techs.Values)
            {
                if (rs.Unlocked.Contains(t.Id)) continue;
                var row = UiKit.Row(list, 40f);
                string cost = ""; foreach (var kv in t.CellCost) cost += kv.Value + " " + (Root.Db.Item(kv.Key)?.Name ?? kv.Key) + " ";
                UiKit.Text_(row.transform, $"{t.Name}  [{t.Branch} T{t.Tier}]  {cost.Trim()}", 16, TextAnchor.MiddleLeft);
                bool can = rs.CanQueue(t.Id, out string why);
                var b = UiKit.Button_(row.transform, can ? "Queue" : why, () => { rs.Enqueue(t.Id); Refresh(); }, new Vector2(150f, 30f));
                b.interactable = can;
            }
            if (rs.Unlocked.Count > 0)
            {
                var row = UiKit.Row(list, 30f);
                UiKit.Text_(row.transform, "Researched: " + string.Join(", ", rs.Unlocked), 14, TextAnchor.MiddleLeft, UiKit.Dim);
            }
        }

        void Update() { if (Visible && Root.Sim != null && Root.Sim.Research.Version != shownVersion) Refresh(); }
    }

    // =================================================================================================
    /// <summary>Click a building: shows its state, inventories, recipe choice, power.</summary>
    public sealed class InspectPanel : UiPanel
    {
        public Building Target { get; private set; }
        Text info; Transform body; int lastInvVersion = -1; float lastRefresh;

        public static InspectPanel Create(GameRoot root, Transform parent)
        {
            var p = Make<InspectPanel>(root, parent, "Building", new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(620f, 520f));
            p.info = UiKit.Text_(p.Box.transform, "", 16, TextAnchor.UpperLeft);
            p.body = Section(p.Box.transform, "").transform;
            return p;
        }

        public void Show(Building b) { Target = b; Show(); }
        public override void Hide() { base.Hide(); Target = null; }

        public override void Refresh()
        {
            if (Target == null || Target.Removed) { Hide(); return; }
            var b = Target; var db = Root.Db;
            string power = b.Def.PowerUse > 0 ? $"  power {b.PowerSatisfaction / 10}%" : b is GeneratorBuilding g ? $"  output {UiKit.Watts(g.CurrentOutput())}" : "";
            info.text = $"{b.Def.Name}   HP {b.Health}/{b.Def.MaxHealth}   {b.State}{power}   facing {b.Facing}";
            Box.transform.Find("Row/Text").GetComponent<Text>().text = b.Def.Name;
            bool full = Time.unscaledTime - lastRefresh > 0.5f;
            switch (b)
            {
                case CrafterBuilding c:
                    if (!full && c.Inputs.Version + c.Outputs.Version == lastInvVersion) return;
                    lastInvVersion = c.Inputs.Version + c.Outputs.Version;
                    UiKit.Clear(body);
                    var recipes = Root.Sim.AvailableRecipes(b.Def.CrafterType);
                    var rrow = UiKit.Row(body, 36f);
                    UiKit.Text_(rrow.transform, "Recipe: " + (c.Recipe(db)?.Name ?? "none") + (recipes.Count == 0 ? "   (no recipes exist yet)" : ""), 16, TextAnchor.MiddleLeft);
                    foreach (var r in recipes)
                    {
                        var rr = UiKit.Row(body, 32f);
                        UiKit.Button_(rr.transform, r.Name, () => { c.SetRecipe(Root.Sim, r.Id, Root.Sim.LocalPlayer?.Inventory); lastInvVersion = -1; Refresh(); }, new Vector2(240f, 28f));
                    }
                    if (c.RecipeId != null) UiKit.Button_(body, "Clear recipe", () => { c.SetRecipe(Root.Sim, null, Root.Sim.LocalPlayer?.Inventory); lastInvVersion = -1; Refresh(); }, new Vector2(160f, 28f));
                    UiKit.Text_(body, $"Progress {c.ProgressPermille(db) / 10}%" + (c.MissingInput ? "   MISSING INPUT" : ""), 16);
                    InvRow("Inputs", c.Inputs); InvRow("Outputs", c.Outputs);
                    break;
                case ChestBuilding ch:
                    if (!full && ch.Storage.Version == lastInvVersion) return;
                    lastInvVersion = ch.Storage.Version; UiKit.Clear(body);
                    InvRow("Contents (click to take)", ch.Storage, true);
                    UiKit.Button_(body, "Put all my items here", () => { Root.Sim.LocalPlayer?.Inventory.TransferAllTo(ch.Storage); lastInvVersion = -1; Refresh(); }, new Vector2(260f, 28f));
                    break;
                case DataCenterBuilding dc:
                    if (!full) return; UiKit.Clear(body);
                    UiKit.Text_(body, $"Heat {dc.HeatPermille / 10}%  cooling x{dc.CoolingPerTickPermille / Mathf.Max(1, dc.Def.HeatPerTick) / 1000f:0.00}" + (dc.Overheated ? "   OVERHEATED" : "") + (dc.MissingCells ? "   needs data cells" : ""), 16);
                    InvRow("Data cells (click to take)", dc.Cells, true);
                    UiKit.Button_(body, "Insert my data cells", () => { var inv = Root.Sim.LocalPlayer?.Inventory; if (inv == null) return; foreach (var it in db.Items.Values) if (it.IsDataCell) { int n = inv.Count(it.Id); if (n > 0) { int left = dc.Cells.Add(it.Id, n); inv.Remove(it.Id, n - left); } } Refresh(); }, new Vector2(260f, 28f));
                    break;
                case DrillBuilding d:
                    if (!full) return; UiKit.Clear(body);
                    UiKit.Text_(body, "Mining: " + (d.Minable.Count == 0 ? "nothing under it" : string.Join(", ", d.Minable)) + (d.Buffered != null ? "   output blocked" : ""), 16);
                    UiKit.Text_(body, "Output side: " + b.Facing + " (the arrow). Put a tube or chest there.", 16, TextAnchor.UpperLeft, UiKit.Dim);
                    break;
                case GeneratorBuilding gen:
                    if (!full) return; UiKit.Clear(body);
                    UiKit.Text_(body, $"Reactor output {UiKit.Watts(gen.CurrentOutput())} of {UiKit.Watts(gen.Def.PowerOutput)} — it is dying.", 16);
                    var net = Root.Sim.NetworkOf(b);
                    if (net != null) UiKit.Text_(body, $"Network: {UiKit.Watts(net.Supply)} supply / {UiKit.Watts(net.Demand)} demand, {net.ConsumerCount} consumers", 16, TextAnchor.UpperLeft, UiKit.Dim);
                    else UiKit.Text_(body, "Not connected: place a wire or tube touching it.", 16, TextAnchor.UpperLeft, UiKit.Dim);
                    break;
                default:
                    if (!full) return; UiKit.Clear(body);
                    if (b is ConveyorBuilding cv) UiKit.Text_(body, $"{cv.Items.Count} items on this tile", 16);
                    if (b is ArmBuilding arm) UiKit.Text_(body, "Holding: " + (arm.Holding ?? "nothing") + ". Takes from behind, drops in front.", 16);
                    break;
            }
            lastRefresh = Time.unscaledTime;
        }

        void InvRow(string title, Inventory inv, bool takeOnClick = false)
        {
            var sec = Section(body, title);
            var grid = SlotGrid(sec.transform, 8).transform;
            for (int i = 0; i < inv.SlotCount; i++)
            {
                int idx = i; var s = inv.Slots[i];
                Slot(grid, s, takeOnClick && s != null ? () =>
                {
                    var pl = Root.Sim.LocalPlayer; if (pl == null) return;
                    int n = s.Count; int left = pl.Inventory.Add(s.Item, n); inv.Remove(s.Item, n - left); lastInvVersion = -1; Refresh();
                } : (Action)null);
            }
        }
    }

    // =================================================================================================
    public sealed class SettingsPanel : UiPanel
    {
        HudController hud; Transform bindings; Text scaleText;

        public static SettingsPanel Create(GameRoot root, Transform parent, HudController hud)
        {
            var p = Make<SettingsPanel>(root, parent, "Settings", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 760f));
            p.hud = hud;
            var row = UiKit.Row(p.Box.transform, 40f);
            p.scaleText = UiKit.Text_(row.transform, "UI scale", 18, TextAnchor.MiddleLeft);
            UiKit.Button_(row.transform, "-", () => { hud.SetUiScale(PlayerPrefs.GetFloat("ng.uiscale", 1f) - 0.1f); p.Refresh(); }, new Vector2(40f, 32f));
            UiKit.Button_(row.transform, "+", () => { hud.SetUiScale(PlayerPrefs.GetFloat("ng.uiscale", 1f) + 0.1f); p.Refresh(); }, new Vector2(40f, 32f));
            UiKit.Text_(p.Box.transform, "Gore: n/a yet.  Keys: click a key to rebind, Esc cancels.", 14, TextAnchor.MiddleLeft, UiKit.Dim);
            p.bindings = Section(p.Box.transform, "Key bindings").transform;
            UiKit.Button_(p.Box.transform, "Reset keys to default", () => { root.Input.ResetBindings(); p.Refresh(); }, new Vector2(240f, 32f));
            return p;
        }

        public override void Refresh()
        {
            scaleText.text = "UI scale " + PlayerPrefs.GetFloat("ng.uiscale", 1f).ToString("0.0");
            for (int i = bindings.childCount - 1; i >= 1; i--) Destroy(bindings.GetChild(i).gameObject);
            foreach (var a in Root.Input.RebindableActions())
            {
                var act = a;
                var row = UiKit.Row(bindings, 30f);
                UiKit.Text_(row.transform, act.name, 16, TextAnchor.MiddleLeft);
                var b = UiKit.Button_(row.transform, Root.Input.BindingDisplay(act), null, new Vector2(200f, 26f));
                var btn = b;
                b.onClick.AddListener(() => { btn.GetComponentInChildren<Text>().text = "press a key..."; Root.Input.Rebind(act, ok => Refresh()); });
            }
        }
    }

    // =================================================================================================
    /// <summary>Terminal-style boot sequence then the menu. Placeholder for the owner's design.</summary>
    public sealed class MainMenuPanel : MonoBehaviour
    {
        GameRoot root; HudController hud; GameObject box; Text boot; Transform buttons; float bootStart; bool bootDone;
        bool ironman; string difficulty = "normal"; int slot = 1; ulong seed; Text optionsText;

        public static MainMenuPanel Create(GameRoot root, Transform parent, HudController hud)
        {
            var go = UiKit.Panel_(parent, "MainMenu", new Color(0.02f, 0.02f, 0.03f, 1f)); UiKit.Stretch(go);
            var p = go.AddComponent<MainMenuPanel>(); p.root = root; p.hud = hud; p.box = go;
            p.boot = UiKit.Text_(go.transform, "", 22, TextAnchor.UpperLeft, UiKit.Accent);
            UiKit.Place(p.boot.gameObject, new Vector2(0f, 1f), new Vector2(60f, -60f), new Vector2(1200f, 300f));
            var col = new GameObject("Buttons", typeof(RectTransform)); col.transform.SetParent(go.transform, false);
            UiKit.Place(col, new Vector2(0f, 0.5f), new Vector2(60f, -40f), new Vector2(520f, 560f));
            UiKit.VList(col, 8, 0);
            p.buttons = col.transform;
            p.seed = (ulong)root.Db.Balance.MapSeedDefault;
            return p;
        }

        public void Show() { box.SetActive(true); bootStart = Time.unscaledTime; bootDone = false; UiKit.Clear(buttons); }
        public void Hide() => box.SetActive(false);

        void Update()
        {
            if (!box.activeSelf) return;
            string l1 = root.Db.Text("boot.line1", "NEW GENERATION"), l2 = root.Db.Text("boot.line2", ""), l3 = root.Db.Text("boot.line3", "");
            string all = l1 + "\n" + l2 + "\n" + l3 + "\n\n> ready_";
            float t = Time.unscaledTime - bootStart;
            int chars = Mathf.Min(all.Length, (int)(t * 60f));
            boot.text = all.Substring(0, chars);
            if (!bootDone && (chars >= all.Length || root.Input.Pause.WasPressedThisFrame() || root.Input.Fire.WasPressedThisFrame()))
            { bootDone = true; boot.text = all; BuildButtons(); }
        }

        void BuildButtons()
        {
            UiKit.Clear(buttons);
            var db = root.Db;
            optionsText = UiKit.Text_(buttons, "", 16, TextAnchor.MiddleLeft, UiKit.Dim);
            UiKit.Button_(buttons, db.Text("ui.new_game", "New game"), () => root.NewGame(slot, ironman, difficulty, seed));
            bool any = false; for (int i = 1; i <= GameRoot.SlotCount; i++) if (root.SlotExists(i)) any = true;
            var cont = UiKit.Button_(buttons, db.Text("ui.continue", "Continue") + " slot " + slot, () => { if (!root.LoadGame(slot)) hud.Toast("Slot " + slot + " is empty"); });
            cont.interactable = any;
            var slotRow = UiKit.Row(buttons, 44f);
            UiKit.Text_(slotRow.transform, "Slot", 18, TextAnchor.MiddleLeft);
            for (int i = 1; i <= GameRoot.SlotCount; i++)
            {
                int s = i;
                UiKit.Button_(slotRow.transform, (root.SlotExists(i) ? "*" : "") + i, () => { slot = s; BuildButtons(); }, new Vector2(56f, 36f), slot == i ? UiKit.Accent : UiKit.PanelLight);
            }
            UiKit.Button_(buttons, db.Text("ui.ironman", "Ironman") + ": " + (ironman ? "ON" : "off"), () => { ironman = !ironman; BuildButtons(); });
            UiKit.Button_(buttons, db.Text("ui.difficulty", "Difficulty") + ": " + difficulty, () => { difficulty = difficulty == "normal" ? "hard" : difficulty == "hard" ? "brutal" : "normal"; BuildButtons(); });
            UiKit.Button_(buttons, "Seed: " + seed + "  (click to randomise)", () => { seed = (ulong)UnityEngine.Random.Range(1, int.MaxValue); BuildButtons(); });
            UiKit.Button_(buttons, db.Text("ui.settings", "Settings"), () => hud.Settings.Toggle());
            UiKit.Button_(buttons, db.Text("ui.quit", "Quit"), () => root.QuitGame());
            optionsText.text = root.SlotExists(slot) ? "Slot " + slot + " last saved " + root.SlotTime(slot).ToString("g") : "Slot " + slot + " is empty";
        }
    }
}
