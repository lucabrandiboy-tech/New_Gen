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
            return t;
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
            if (def.IsConveyor) return SpriteRegistry.Get("bld_" + def.Id + "_straight");
            if (def.Kind == BuildingKind.Arm) return SpriteRegistry.Get("bld_arm");
            return SpriteRegistry.Get("bld_" + def.Id + "_idle");
        }

        public void Rotate() { if (Selected != null && root.Db.Building(Selected).Rotatable) Facing = Facing.RotateCW(); }

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
                if (!ui && inp.Build.WasPressedThisFrame() && HoverBuilding != null) root.Hud.Inspect(HoverBuilding);
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
            bool rotates = def.IsConveyor || def.Kind == BuildingKind.Arm;
            ghost.position = WorldSpace.TileCorner(origin) + (rotates ? new Vector3(0.5f, 0.5f, 0f) : Vector3.zero);
            ghost.rotation = rotates ? Quaternion.Euler(0f, 0f, WorldSpace.FacingAngle(Facing)) : Quaternion.identity;
            // sprites have a bottom-left pivot; for rotating 1x1 pieces offset so they spin around the tile centre
            ghostSprite.transform.localPosition = rotates ? new Vector3(-0.5f, -0.5f, 0f) : Vector3.zero;
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
