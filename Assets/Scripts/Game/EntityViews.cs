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
        Sprite[] frames; float animTime;

        /// <summary>
        /// Which animation frame to show. "loop" runs at the building's own fps; "progress"
        /// stretches the frames across the current craft so the last frame lands exactly when
        /// the item is finished (what the owner asked for on the furnace).
        /// </summary>
        int FrameIndex(float dt)
        {
            if (B.Def.AnimationMode == "progress" && B is CrafterBuilding c)
            {
                int p = Mathf.Clamp(c.ProgressPermille(Root.Db), 0, 999);
                return p * frames.Length / 1000;
            }
            animTime += dt;
            float fps = B.Def.AnimationFps > 0 ? B.Def.AnimationFps : 10f;
            return (int)(animTime * fps) % frames.Length;
        }
        readonly List<SpriteRenderer> itemSprites = new List<SpriteRenderer>();
        SpriteRenderer[] brokenTiles;   // shared damage/rubble art laid over every tile of the footprint
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
            if (B is ArmBuilding) { hand = MakeSprite(transform, "hand", body.sortingOrder + 1); hand.enabled = false; }
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
            pivot.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            body.transform.localPosition = new Vector3(-B.Width / 2f, -B.Height / 2f, 0f);

            if (B is ConveyorBuilding conv) { RefreshConveyor(conv, id); return; }
            if (B is ArmBuilding) { body.sprite = SpriteRegistry.Get("bld_arm_idle"); ApplyStateTint(); return; }

            switch (B.State)
            {
                case BuildingState.Destroyed:
                    // One shared rubble sprite replaces the building, tiled over its footprint.
                    body.sprite = Find("bld_" + id + "_destroyed") ?? SpriteRegistry.Get("bld_" + id + "_idle");
                    break;
                case BuildingState.Damaged:
                    // Idle art stays; one shared damage sprite is laid on top, tiled over the footprint.
                    body.sprite = Find("bld_" + id + "_damaged") ?? SpriteRegistry.Get("bld_" + id + "_idle");
                    break;
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
            ApplyBrokenOverlay(id);
            ApplyStateTint();
        }

        /// <summary>
        /// The owner draws one damaged sprite and one destroyed sprite for the whole game instead of a
        /// pair per building. They are 1 tile each and get repeated over the building's footprint.
        /// A building that does have its own bld_&lt;id&gt;_damaged / _destroyed art uses that instead.
        /// </summary>
        void ApplyBrokenOverlay(string id)
        {
            bool destroyed = B.State == BuildingState.Destroyed;
            bool damaged = B.State == BuildingState.Damaged;
            bool ownArt = Find("bld_" + id + (destroyed ? "_destroyed" : "_damaged")) != null;
            bool show = (destroyed || damaged) && !ownArt;

            if (!show)
            {
                if (brokenTiles != null) foreach (var t in brokenTiles) t.enabled = false;
                body.enabled = true;
                return;
            }

            var sprite = SpriteRegistry.Get(destroyed ? "bld_destroyed" : "bld_damaged");
            body.enabled = !destroyed;                      // rubble replaces the building entirely
            if (brokenTiles == null)
            {
                brokenTiles = new SpriteRenderer[B.Width * B.Height];
                for (int i = 0; i < brokenTiles.Length; i++)
                {
                    var sr = MakeSprite(transform, "broken" + i, body.sortingOrder + (destroyed ? 1 : 3));
                    sr.transform.localPosition = new Vector3(i % B.Width, i / B.Width, 0f);
                    brokenTiles[i] = sr;
                }
            }
            foreach (var t in brokenTiles)
            {
                t.enabled = true;
                t.sprite = sprite;
                t.sortingOrder = body.sortingOrder + (destroyed ? 1 : 3);
            }
        }

        /// <summary>
        /// Conveyors prefer one drawing per direction (bld_tube_right, _left, _up, _down) so the
        /// top-left lighting stays correct. If a direction has no drawing we fall back to a single
        /// bld_tube_straight / _corner that gets rotated.
        /// </summary>
        void RefreshConveyor(ConveyorBuilding conv, string id)
        {
            var perDir = SpriteRegistry.TryGetFrames("bld_" + id + "_" + B.Facing.Name());
            if (perDir != null && perDir.Length > 0)
            {
                body.sprite = perDir[0];
                frames = perDir.Length > 1 ? perDir : null;
            }
            else
            {
                bool corner = conv.FeedFrom.HasValue && conv.FeedFrom.Value != B.Facing.Opposite();
                body.sprite = SpriteRegistry.Get("bld_" + id + (corner ? "_corner" : "_straight"));
                pivot.localRotation = Quaternion.Euler(0f, 0f, WorldSpace.FacingAngle(B.Facing));
                if (corner && conv.FeedFrom.Value == B.Facing.RotateCW())
                {
                    body.transform.localScale = new Vector3(-1f, 1f, 1f);
                    body.transform.localPosition = new Vector3(B.Width / 2f, -B.Height / 2f, 0f);
                }
            }
            lastFeed = conv.FeedFrom; lastFacing = B.Facing;
            ApplyStateTint();
        }

        void ApplyStateTint()
        {
            body.color = B.State == BuildingState.NoPower ? new Color(0.6f, 0.6f, 0.7f) : Color.white;
            icon.enabled = B.State == BuildingState.NoPower
                        || (B is CrafterBuilding c && c.MissingInput)
                        || (B is DataCenterBuilding d && (d.MissingCells || d.Overheated));
            if (!icon.enabled) return;
            string k = B.State == BuildingState.NoPower ? "icon_nopower"
                     : (B is DataCenterBuilding dd && dd.Overheated ? "icon_overheat" : "icon_missing_input");
            icon.sprite = SpriteRegistry.Get(k);
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
            if (frames != null) body.sprite = frames[FrameIndex(dt)];
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
            var from = WorldSpace.TileCenter(arm.InputTile) - transform.position;
            var to = WorldSpace.TileCenter(arm.OutputTile) - transform.position;
            float t = Mathf.Clamp01(arm.FlightPermille / 1000f);
            hand.transform.localPosition = Vector3.Lerp(from, to, t) + new Vector3(0f, 0.25f, 0f);
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
