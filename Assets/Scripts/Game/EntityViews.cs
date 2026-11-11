using System.Collections.Generic;
using NewGen.Art;
using NewGen.Sim;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NewGen.Game
{
    /// <summary>Base for anything drawn for a sim entity.</summary>
    public abstract class EntityView : MonoBehaviour
    {
        public Entity Entity;
        protected GameRoot Root;
        public virtual void Init(GameRoot root, Entity e) { Root = root; Entity = e; }
        /// <summary>Called when the sim says the entity's state changed.</summary>
        public virtual void OnStateChanged() { }
        /// <summary>Called every rendered frame.</summary>
        public abstract void Tick(float dt);

        protected static SpriteRenderer MakeSprite(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }
    }

    // ------------------------------------------------------------------------------------------------
    public sealed class BuildingView : EntityView
    {
        public Building B;
        Transform pivot;
        SpriteRenderer body, icon, hand;
        Light2D glow;
        Sprite[] frames; float animTime; const float AnimFps = 10f;   // 0.10 s per frame
        readonly List<SpriteRenderer> itemSprites = new List<SpriteRenderer>();
        int lastItemsVersion = -1, lastStateVersion = -1;
        Dir? lastFeed; Dir lastFacing;

        public override void Init(GameRoot root, Entity e)
        {
            base.Init(root, e);
            B = (Building)e;
            transform.position = WorldSpace.TileCorner(B.Origin);
            pivot = new GameObject("pivot").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(B.Width / 2f, B.Height / 2f, 0f);
            body = MakeSprite(pivot, "body", WorldSpace.SortOrder(B.Origin.Y));
            body.transform.localPosition = new Vector3(-B.Width / 2f, -B.Height / 2f, 0f);
            icon = MakeSprite(transform, "icon", body.sortingOrder + 2);
            icon.transform.localPosition = new Vector3(B.Width / 2f, B.Height / 2f + 0.3f, 0f);
            icon.enabled = false;
            if (B is ArmBuilding) { hand = MakeSprite(pivot, "hand", body.sortingOrder + 1); hand.enabled = false; }
            if (B.Def.Kind != Sim.Content.BuildingKind.Tube && B.Def.Kind != Sim.Content.BuildingKind.Wire && B.Def.Kind != Sim.Content.BuildingKind.Chest)
            {
                var lgo = new GameObject("glow"); lgo.transform.SetParent(pivot, false);
                glow = lgo.AddComponent<Light2D>();
                glow.lightType = Light2D.LightType.Point;
                glow.color = new Color(1f, 0.6f, 0.25f);
                glow.intensity = 1.1f;
                glow.pointLightOuterRadius = Mathf.Max(B.Width, B.Height) * 0.9f;
                glow.pointLightInnerRadius = 0.2f;
                glow.enabled = false;
            }
            Refresh();
        }

        public override void OnStateChanged() => Refresh();

        void Refresh()
        {
            lastStateVersion = B.StateVersion;
            string id = B.Def.Id;
            frames = null;
            bool rotates = B.Def.IsConveyor || B.Def.Kind == Sim.Content.BuildingKind.Arm;
            pivot.localRotation = rotates ? Quaternion.Euler(0f, 0f, WorldSpace.FacingAngle(B.Facing)) : Quaternion.identity;
            body.transform.localScale = Vector3.one;
            body.transform.localPosition = new Vector3(-B.Width / 2f, -B.Height / 2f, 0f);

            if (B is ConveyorBuilding conv)
            {
                bool corner = conv.FeedFrom.HasValue && conv.FeedFrom.Value != B.Facing.Opposite();
                string key = "bld_" + id + (corner ? "_corner" : "_straight");
                body.sprite = SpriteRegistry.Get(key);
                // corner sprite enters from the left; mirror it when fed from the right
                if (corner && conv.FeedFrom.Value == B.Facing.RotateCW())
                {
                    body.transform.localScale = new Vector3(-1f, 1f, 1f);
                    body.transform.localPosition = new Vector3(B.Width / 2f, -B.Height / 2f, 0f);
                }
                lastFeed = conv.FeedFrom; lastFacing = B.Facing;
                ApplyStateTint();
                return;
            }
            if (B is ArmBuilding) { body.sprite = SpriteRegistry.Get("bld_arm"); ApplyStateTint(); return; }

            switch (B.State)
            {
                case BuildingState.Destroyed: body.sprite = SpriteRegistry.Get("bld_" + id + "_destroyed"); break;
                case BuildingState.Damaged: body.sprite = SpriteRegistry.Get("bld_" + id + "_damaged"); break;
                case BuildingState.NoPower: body.sprite = Find("bld_" + id + "_nopower") ?? SpriteRegistry.Get("bld_" + id + "_idle"); break;
                case BuildingState.Working:
                    {
                        frames = SpriteRegistry.TryGetFrames("bld_" + id + "_working_" + B.Facing.Name())
                              ?? SpriteRegistry.TryGetFrames("bld_" + id + "_working");
                        body.sprite = frames != null && frames.Length > 0 ? frames[0] : SpriteRegistry.Get("bld_" + id + "_idle");
                        if (frames != null && frames.Length <= 1) frames = null;
                        break;
                    }
                default: body.sprite = SpriteRegistry.Get("bld_" + id + "_idle"); break;
            }
            if (glow != null) glow.enabled = B.State == BuildingState.Working;
            ApplyStateTint();
        }

        void ApplyStateTint()
        {
            body.color = B.State == BuildingState.NoPower ? new Color(0.6f, 0.6f, 0.7f) : Color.white;
            icon.enabled = B.State == BuildingState.NoPower || (B is CrafterBuilding c && c.MissingInput) || (B is DataCenterBuilding d && (d.MissingCells || d.Overheated));
            if (icon.enabled)
            {
                string k = B.State == BuildingState.NoPower ? "icon_nopower" : (B is DataCenterBuilding dd && dd.Overheated ? "icon_overheat" : "icon_missing_input");
                icon.sprite = SpriteRegistry.Get(k);
            }
        }

        static Sprite Find(string key) => SpriteRegistry.TryGet(key);

        public override void Tick(float dt)
        {
            if (B.StateVersion != lastStateVersion) Refresh();
            if (B is ConveyorBuilding conv)
            {
                if (conv.FeedFrom != lastFeed || B.Facing != lastFacing) Refresh();
                UpdateConveyorItems(conv);
                return;
            }
            if (B is ArmBuilding arm) { UpdateArm(arm); return; }
            if (frames != null)
            {
                animTime += dt;
                body.sprite = frames[(int)(animTime * AnimFps) % frames.Length];
            }
            // icon flashes
            if (icon.enabled) icon.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Sin(Time.time * 6f));
            if (B is CrafterBuilding cb && icon.enabled != (cb.MissingInput || B.State == BuildingState.NoPower)) ApplyStateTint();
            if (B is DataCenterBuilding dc && icon.enabled != (dc.MissingCells || dc.Overheated || B.State == BuildingState.NoPower)) ApplyStateTint();
        }

        void UpdateConveyorItems(ConveyorBuilding conv)
        {
            if (conv.ItemsVersion == lastItemsVersion) return;
            lastItemsVersion = conv.ItemsVersion;
            while (itemSprites.Count < conv.Items.Count)
            {
                var sr = MakeSprite(transform, "item", body.sortingOrder + 1);
                sr.transform.localScale = Vector3.one * 0.8f;
                itemSprites.Add(sr);
            }
            var d = B.Facing.Delta();
            for (int i = 0; i < itemSprites.Count; i++)
            {
                var sr = itemSprites[i];
                if (i >= conv.Items.Count) { sr.enabled = false; continue; }
                var it = conv.Items[i];
                sr.enabled = true;
                var def = Root.Db.Item(it.Item);
                sr.sprite = SpriteRegistry.Get(def != null ? def.SpriteKey : "item_" + it.Item);
                float t = it.Pos / (float)Fix.SubPerTile - 0.5f;
                sr.transform.localPosition = new Vector3(0.5f + d.X * t, 0.5f + d.Y * t, 0f);
            }
        }

        void UpdateArm(ArmBuilding arm)
        {
            bool show = arm.Holding != null;
            hand.enabled = show;
            if (!show) return;
            var def = Root.Db.Item(arm.Holding);
            hand.sprite = SpriteRegistry.Get(def != null ? def.SpriteKey : "item_" + arm.Holding);
            hand.transform.localScale = Vector3.one * 0.7f;
            float full = arm.Def.ArmTicksPerSwing * 1000f;
            float t = Mathf.Clamp01(arm.SwingProgress / full);        // 0 = over the back tile, 1 = over the front tile
            hand.transform.localPosition = new Vector3(0f, Mathf.Lerp(-1f, 1f, t), 0f);   // pivot is rotated with the arm
        }
    }

    // ------------------------------------------------------------------------------------------------
    public sealed class PlayerView : EntityView
    {
        public Player P;
        SpriteRenderer body;
        Vector3 shown; bool first = true; float animTime;
        SpriteRenderer mineBar;

        public override void Init(GameRoot root, Entity e)
        {
            base.Init(root, e);
            P = (Player)e;
            body = MakeSprite(transform, "body", 0);
            mineBar = MakeSprite(transform, "minebar", 5000);
            mineBar.enabled = false;
        }

        public override void Tick(float dt)
        {
            var target = WorldSpace.ToUnity(P.Pos);
            if (first) { shown = target; first = false; }
            float k = 1f - Mathf.Exp(-18f * dt);
            shown = Vector3.Lerp(shown, target, k);
            if ((shown - target).sqrMagnitude > 9f) shown = target;   // teleport
            transform.position = shown;
            body.sortingOrder = WorldSpace.SortOrder(shown.y - 0.5f);

            string dir = P.Facing == Dir.North ? "up" : P.Facing == Dir.South ? "down" : "side";
            body.flipX = P.Facing == Dir.West;
            if (P.Moving)
            {
                animTime += dt;
                var frames = SpriteRegistry.GetFrames("unit_player_walk_" + dir);
                body.sprite = frames != null && frames.Length > 0 ? frames[(int)(animTime * 8f) % frames.Length] : SpriteRegistry.Get("unit_player_idle_" + dir);
            }
            else { animTime = 0f; body.sprite = SpriteRegistry.Get("unit_player_idle_" + dir); }
            body.color = P.Dead ? new Color(0.4f, 0.4f, 0.5f) : Color.white;
        }
    }

    // ------------------------------------------------------------------------------------------------
    public sealed class ProjectileView : EntityView
    {
        Projectile pr; SpriteRenderer body;
        public override void Init(GameRoot root, Entity e)
        {
            base.Init(root, e);
            pr = (Projectile)e;
            body = MakeSprite(transform, "body", 4000);
            body.sprite = SpriteRegistry.Get("fx_bullet");
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pr.VelY, pr.VelX) * Mathf.Rad2Deg);
        }
        public override void Tick(float dt) { transform.position = WorldSpace.ToUnity(pr.Pos); }
    }

    // ------------------------------------------------------------------------------------------------
    /// <summary>Creates/destroys views as entities come and go, and ticks them every frame.</summary>
    public sealed class EntityViewManager : MonoBehaviour
    {
        GameRoot root;
        readonly Dictionary<int, EntityView> views = new Dictionary<int, EntityView>();
        readonly List<EntityView> list = new List<EntityView>();
        public Transform PlayerTransform { get; private set; }

        public static EntityViewManager Create(GameRoot root)
        {
            var go = new GameObject("Entities");
            var m = go.AddComponent<EntityViewManager>();
            m.root = root;
            return m;
        }

        public void Clear()
        {
            foreach (var v in list) if (v != null) Destroy(v.gameObject);
            views.Clear(); list.Clear(); PlayerTransform = null;
        }

        public void Rebuild()
        {
            Clear();
            if (root.Sim == null) return;
            foreach (var e in root.Sim.AllEntities) Add(e);
            root.Sim.Events.Clear();
        }

        public EntityView Get(int id) => views.TryGetValue(id, out var v) ? v : null;

        void Add(Entity e)
        {
            if (e == null || views.ContainsKey(e.Id)) return;
            var go = new GameObject(e.Kind + " " + e.Id);
            go.transform.SetParent(transform, false);
            EntityView v;
            switch (e.Kind)
            {
                case EntityKind.Player: v = go.AddComponent<PlayerView>(); break;
                case EntityKind.Projectile: v = go.AddComponent<ProjectileView>(); break;
                default: v = go.AddComponent<BuildingView>(); break;
            }
            v.Init(root, e);
            views[e.Id] = v; list.Add(v);
            if (e.Kind == EntityKind.Player && PlayerTransform == null) { PlayerTransform = go.transform; root.Camera.Target = go.transform; }
        }

        void Remove(int id)
        {
            if (!views.TryGetValue(id, out var v)) return;
            views.Remove(id); list.Remove(v);
            if (v != null) Destroy(v.gameObject);
        }

        public void HandleEvent(SimEvent e)
        {
            switch (e.Type)
            {
                case SimEventType.EntityAdded: Add(root.Sim.GetEntity(e.EntityId)); break;
                case SimEventType.EntityRemoved: Remove(e.EntityId); break;
                case SimEventType.EntityStateChanged: Get(e.EntityId)?.OnStateChanged(); break;
                case SimEventType.TileChanged: root.Terrain.RedrawTile(e.Tile); break;
            }
        }

        public void Refresh()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < list.Count; i++) if (list[i] != null) list[i].Tick(dt);
        }
    }
}
