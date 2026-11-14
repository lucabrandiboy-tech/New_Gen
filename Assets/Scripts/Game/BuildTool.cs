using NewGen.Art;
using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using UnityEngine;

namespace NewGen.Game
{
    /// <summary>
    /// Ghost preview + placing + deconstructing.
    ///  - Pick a building in the quickbar (1-0). Left click places it; R rotates; Q clears the selection.
    ///  - With a tube/wire selected, hold left click and drag to lay a line.
    ///  - Right click on a building removes it (items go to your inventory). Hold right click on ore/ice to mine by hand.
    ///  - Left click on your own building with nothing selected opens it.
    /// </summary>
    public sealed class BuildTool : MonoBehaviour
    {
        GameRoot root;
        public string Selected { get; private set; }
        public Dir Facing { get; private set; } = Dir.North;
        public bool HasSelection => Selected != null;
        public bool MineHeld { get; private set; }
        public Building HoverBuilding { get; private set; }
        public TilePos HoverTile { get; private set; }

        Transform ghost; SpriteRenderer ghostSprite;
        TilePos lastDragTile; bool dragging;

        // --- arm wiring mode: click the arm, then click its input tile and its output tile
        ArmBuilding configuringArm;
        Dir? pendingInput;
        SpriteRenderer[] armTiles;
        static readonly Dir[] Sides = { Dir.North, Dir.East, Dir.South, Dir.West };
        static readonly Color SideFree = new Color(0.30f, 1f, 0.45f, 0.35f);
        static readonly Color SideInput = new Color(0.35f, 0.65f, 1f, 0.55f);
        static readonly Color SideCurrent = new Color(0.30f, 1f, 0.45f, 0.18f);
        public bool ConfiguringArm => configuringArm != null;

        public static BuildTool Create(GameRoot root)
        {
            var go = new GameObject("BuildTool");
            var t = go.AddComponent<BuildTool>();
            t.root = root;
            t.ghost = new GameObject("ghost").transform;
            t.ghost.SetParent(go.transform, false);
            t.ghostSprite = t.ghost.gameObject.AddComponent<SpriteRenderer>();
            t.ghostSprite.sortingOrder = 3000;
            t.ghost.gameObject.SetActive(false);
            t.armTiles = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                var q = new GameObject("armSide" + i);
                q.transform.SetParent(go.transform, false);
                var sr = q.AddComponent<SpriteRenderer>();
                sr.sprite = WhiteSprite();
                sr.sortingOrder = 2900;
                q.SetActive(false);
                t.armTiles[i] = sr;
            }
            return t;
        }

        static Sprite white;
        /// <summary>A plain white 1x1 sprite, used to tint whole tiles for the arm overlay.</summary>
        static Sprite WhiteSprite()
        {
            if (white != null) return white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixels32(new[] { new Color32(255, 255, 255, 255) });
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero, 1f);
            return white;
        }

        // ------------------------------------------------------------------ arm wiring
        public void BeginArmConfig(ArmBuilding arm)
        {
            configuringArm = arm; pendingInput = null;
            Select(null);
            for (int i = 0; i < 4; i++)
            {
                var tile = arm.Origin.Offset(Sides[i]);
                armTiles[i].gameObject.SetActive(true);
                armTiles[i].transform.position = WorldSpace.TileCorner(tile);
                bool current = arm.InputSide == Sides[i] || arm.OutputSide == Sides[i];
                armTiles[i].color = current ? SideCurrent : SideFree;
            }
            root.Hud.Toast("Click the tile to take FROM, then the tile to put INTO.  Q cancels.");
        }

        public void CancelArmConfig()
        {
            configuringArm = null; pendingInput = null;
            if (armTiles != null) foreach (var q in armTiles) q.gameObject.SetActive(false);
        }

        /// <returns>true while the arm overlay is handling input.</returns>
        bool UpdateArmConfig(bool uiBlocked)
        {
            if (configuringArm == null) return false;
            if (configuringArm.Removed) { CancelArmConfig(); return false; }
            var inp = root.Input;
            if (inp.Cancel.WasPressedThisFrame() || inp.Pause.WasPressedThisFrame()) { CancelArmConfig(); return true; }
            if (uiBlocked || !inp.Build.WasPressedThisFrame()) return true;

            int clicked = -1;
            for (int i = 0; i < 4; i++) if (configuringArm.Origin.Offset(Sides[i]) == HoverTile) clicked = i;
            if (clicked < 0) { CancelArmConfig(); return true; }

            if (pendingInput == null)
            {
                pendingInput = Sides[clicked];
                for (int i = 0; i < 4; i++) armTiles[i].color = i == clicked ? SideInput : SideFree;
                root.Hud.Toast("Input: " + Sides[clicked].Name() + ".  Now click where it should go.");
                return true;
            }
            if (Sides[clicked] == pendingInput.Value) { root.Hud.Toast("Pick a different tile for the output."); return true; }
            root.Sim.ConfigureArm(configuringArm, pendingInput.Value, Sides[clicked]);
            root.Hud.Toast("Arm: " + pendingInput.Value.Name() + " -> " + Sides[clicked].Name());
            CancelArmConfig();
            return true;
        }

        public void Select(string buildingId)
        {
            if (buildingId != null && root.Db.Building(buildingId) == null) return;
            Selected = buildingId;
            var def = buildingId == null ? null : root.Db.Building(buildingId);
            if (def != null && !def.Rotatable) Facing = Dir.North;
            ghost.gameObject.SetActive(def != null);
            if (def != null) ghostSprite.sprite = GhostSprite(def);
            root.Hud.OnSelectionChanged();
        }

        Sprite GhostSprite(BuildingDef def)
        {
            if (def.IsConveyor) return SpriteRegistry.TryGet("bld_" + def.Id + "_" + Facing.Name()) ?? SpriteRegistry.Get("bld_" + def.Id + "_straight");
            if (def.Kind == BuildingKind.Arm) return SpriteRegistry.Get("bld_arm_idle");
            return SpriteRegistry.Get("bld_" + def.Id + "_idle");
        }

        public void Rotate()
        {
            if (Selected == null || !root.Db.Building(Selected).Rotatable) return;
            Facing = Facing.RotateCW();
            ghostSprite.sprite = GhostSprite(root.Db.Building(Selected));
        }

        /// <summary>Origin so that the cursor sits in the middle of the footprint.</summary>
        TilePos OriginFor(BuildingDef def, TilePos cursor)
        {
            Simulation.FootprintSize(def, Facing, out int w, out int h);
            return new TilePos(cursor.X - (w - 1) / 2, cursor.Y - (h - 1) / 2);
        }

        public void UpdateInput()
        {
            var sim = root.Sim; var inp = root.Input;
            if (sim == null) return;
            var world = root.Camera.MouseWorld(inp.MousePosition);
            HoverTile = WorldSpace.ToTile(world);
            HoverBuilding = sim.BuildingAt(HoverTile);
            bool ui = root.Hud.PointerOverUi || root.Console.IsOpen;
            MineHeld = false;
            if (UpdateArmConfig(ui)) return;

            if (!root.Console.IsOpen)
            {
                if (inp.Rotate.WasPressedThisFrame()) Rotate();
                if (inp.Cancel.WasPressedThisFrame()) { if (Selected != null) Select(null); else root.Hud.CloseInspect(); }
                for (int i = 0; i < 10; i++) if (inp.Quickbar[i].WasPressedThisFrame()) root.Hud.PressQuickbar(i);
                if (inp.QuickbarNext.WasPressedThisFrame()) root.Hud.QuickbarPage(1);
                if (inp.QuickbarPrev.WasPressedThisFrame()) root.Hud.QuickbarPage(-1);
                if (inp.QuickSave.WasPressedThisFrame()) root.Hud.TrySave();
                if (inp.Inventory.WasPressedThisFrame()) root.Hud.ToggleInventory();
                if (inp.Research.WasPressedThisFrame()) root.Hud.ToggleResearch();
            }
            if (!ui) root.Camera.Zoom(inp.ZoomDelta);

            var player = sim.LocalPlayer;
            if (Selected != null)
            {
                var def = root.Db.Building(Selected);
                var origin = OriginFor(def, HoverTile);
                Simulation.FootprintSize(def, Facing, out int w, out int h);
                var result = sim.CanPlace(Selected, origin, Facing, player?.Inventory);
                bool inReach = player != null && sim.InReach(player, HoverTile);
                UpdateGhost(def, origin, w, h, result == PlaceResult.Ok && inReach);

                if (!ui && inp.Build.IsPressed())
                {
                    bool pressed = inp.Build.WasPressedThisFrame();
                    bool lineTool = def.IsConveyor || def.Kind == BuildingKind.Arm;
                    if (pressed || (lineTool && dragging && HoverTile != lastDragTile))
                    {
                        if (!inReach) { if (pressed) root.Hud.Toast("Too far away"); }
                        else if (result == PlaceResult.Ok) sim.Place(Selected, origin, Facing, player?.Inventory);
                        else if (pressed) root.Hud.Toast(Reason(result));
                        dragging = lineTool; lastDragTile = HoverTile;
                    }
                }
                else dragging = false;
            }
            else
            {
                ghost.gameObject.SetActive(false);
                if (!ui && inp.Build.WasPressedThisFrame() && HoverBuilding != null)
                {
                    if (HoverBuilding is ArmBuilding arm && root.Sim.LocalPlayer != null && root.Sim.InReach(root.Sim.LocalPlayer, HoverTile)) BeginArmConfig(arm);
                    else root.Hud.Inspect(HoverBuilding);
                }
            }

            if (!ui)
            {
                if (inp.Mine.WasPressedThisFrame() && HoverBuilding != null)
                {
                    if (player != null && sim.InReach(player, HoverTile)) { sim.Remove(HoverBuilding, player.Inventory); root.Hud.CloseInspectIf(HoverBuilding); }
                    else root.Hud.Toast("Too far away");
                }
                else if (inp.Mine.IsPressed() && HoverBuilding == null) MineHeld = true;
            }
        }

        void UpdateGhost(BuildingDef def, TilePos origin, int w, int h, bool ok)
        {
            ghost.gameObject.SetActive(true);
            // Per-direction conveyor drawings are already facing the right way; a single
            // bld_..._straight fallback has to be rotated around the tile centre instead.
            bool rotates = def.IsConveyor && SpriteRegistry.TryGet("bld_" + def.Id + "_" + Facing.Name()) == null;
            ghost.position = WorldSpace.TileCorner(origin) + (rotates ? new Vector3(0.5f, 0.5f, 0f) : Vector3.zero);
            ghost.rotation = rotates ? Quaternion.Euler(0f, 0f, WorldSpace.FacingAngle(Facing)) : Quaternion.identity;
            ghostSprite.transform.localPosition = rotates ? new Vector3(-0.5f, -0.5f, 0f) : Vector3.zero;
            ghostSprite.transform.localScale = Vector3.one;
            ghostSprite.color = ok ? new Color(0.4f, 1f, 0.5f, 0.55f) : new Color(1f, 0.3f, 0.3f, 0.55f);
        }

        static string Reason(PlaceResult r)
        {
            switch (r)
            {
                case PlaceResult.BadTerrain: return "Can't build here";
                case PlaceResult.Occupied: return "Something is in the way";
                case PlaceResult.PlayerInWay: return "You are in the way";
                case PlaceResult.MissingItems: return "Missing materials";
                case PlaceResult.NotUnlocked: return "Not researched yet";
                default: return "Can't place that";
            }
        }
    }
}
