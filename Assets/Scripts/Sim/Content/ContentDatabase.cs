using System;
using System.Collections.Generic;
using System.IO;
using NewGen.Sim.Core;

namespace NewGen.Sim.Content
{
    /// <summary>
    /// All game definitions, loaded from JSON files in a content folder
    /// (Assets/StreamingAssets/Content in the Unity project). Plain files = easy to mod later.
    /// </summary>
    public sealed class ContentDatabase
    {
        public readonly Dictionary<string, ItemDef> Items = new Dictionary<string, ItemDef>();
        public readonly Dictionary<string, RecipeDef> Recipes = new Dictionary<string, RecipeDef>();
        public readonly Dictionary<string, BuildingDef> Buildings = new Dictionary<string, BuildingDef>();
        public readonly Dictionary<string, TechDef> Techs = new Dictionary<string, TechDef>();
        public readonly List<TerrainDef> Terrains = new List<TerrainDef>();     // index = TerrainDef.Index
        public readonly List<ResourceDef> Resources = new List<ResourceDef>();  // index = ResourceDef.Index, [0] = none
        public readonly Dictionary<string, string> Loc = new Dictionary<string, string>();
        public Balance Balance = new Balance();
        public readonly List<string> Problems = new List<string>();

        public static ContentDatabase LoadFromDirectory(string dir)
        {
            var db = new ContentDatabase();
            db.Load(name => { string p = Path.Combine(dir, name); return File.Exists(p) ? File.ReadAllText(p) : null; });
            return db;
        }

        /// <summary>readFile returns the file text or null if missing.</summary>
        public void Load(Func<string, string> readFile)
        {
            Resources.Clear(); Resources.Add(new ResourceDef { Id = "none", Name = "None", Index = 0 });

            var bal = ReadObj(readFile, "balance.json");
            if (bal != null) LoadBalance(bal);

            foreach (var o in ReadList(readFile, "terrain.json", "terrain"))
            {
                var t = new TerrainDef
                {
                    Id = o.Str("id"), Name = o.Str("name", o.Str("id")), Buildable = o.Bool("buildable", true), Walkable = o.Bool("walkable", true),
                    WalkSpeedPermille = o.Int("walkSpeedPermille", 1000), ColdPerTick = o.Int("coldPerTick", 0), CoolingPermille = o.Int("coolingPermille", 1000),
                    YieldsItem = o.Str("yields"), HandMineTicks = o.Int("handMineTicks", 60),
                };
                t.Index = (byte)Terrains.Count;
                Terrains.Add(t);
            }
            foreach (var o in ReadList(readFile, "resources.json", "resources"))
            {
                var r = new ResourceDef
                {
                    Id = o.Str("id"), Name = o.Str("name", o.Str("id")), YieldsItem = o.Str("yields"), HandMineTicks = o.Int("handMineTicks", 60),
                    SpawnWeight = o.Int("spawnWeight", 100), PatchRadiusMin = o.Int("patchRadiusMin", 4), PatchRadiusMax = o.Int("patchRadiusMax", 9),
                };
                r.Index = (byte)Resources.Count;
                Resources.Add(r);
            }
            foreach (var o in ReadList(readFile, "items.json", "items"))
            {
                var it = new ItemDef { Id = o.Str("id"), Name = o.Str("name", o.Str("id")), StackSize = o.Int("stackSize", 100), Category = o.Str("category", "material") };
                Items[it.Id] = it;
            }
            foreach (var o in ReadList(readFile, "recipes.json", "recipes"))
            {
                var r = new RecipeDef
                {
                    Id = o.Str("id"), Name = o.Str("name", o.Str("id")), Ticks = o.Int("ticks", 60), Crafter = o.Str("crafter", "hand"),
                    UnlockedAtStart = o.Bool("unlockedAtStart", true),
                };
                r.Inputs = ReadStacks(o.Obj("inputs"));
                r.Outputs = ReadStacks(o.Obj("outputs"));
                Recipes[r.Id] = r;
            }
            foreach (var o in ReadList(readFile, "buildings.json", "buildings"))
            {
                var b = new BuildingDef
                {
                    Id = o.Str("id"), Name = o.Str("name", o.Str("id")), Width = o.Int("width", 1), Height = o.Int("height", 1),
                    Rotatable = o.Bool("rotatable", true), MaxHealth = o.Int("maxHealth", 100), UnlockedAtStart = o.Bool("unlockedAtStart", true),
                    PowerUse = o.Int("powerUse"), PowerOutput = o.Int("powerOutput"), DecayTicks = o.Int("decayTicks"), DecayFloorPermille = o.Int("decayFloorPermille", 1000),
                    MineTicksPerItem = o.Int("mineTicksPerItem"), CrafterType = o.Str("crafterType"), InputSlots = o.Int("inputSlots", 2), OutputSlots = o.Int("outputSlots", 1),
                    Slots = o.Int("slots"), SubPerTick = o.Int("subPerTick"), ItemsPerTile = o.Int("itemsPerTile", 4), ArmTicksPerSwing = o.Int("armTicksPerSwing", 30),
                    ResearchPerTick = o.Int("researchPerTick"), HeatPerTick = o.Int("heatPerTick"), MaxHeat = o.Int("maxHeat", 1000),
                    WarmthRadiusTiles = o.Int("warmthRadiusTiles"), WarmthPerTick = o.Int("warmthPerTick"),
                };
                if (!Enum.TryParse(o.Str("kind", "Static"), true, out b.Kind)) Problems.Add($"building {b.Id}: unknown kind '{o.Str("kind")}'");
                b.Cost = ReadStacks(o.Obj("cost"));
                Buildings[b.Id] = b;
            }
            foreach (var o in ReadList(readFile, "techs.json", "techs"))
            {
                var t = new TechDef
                {
                    Id = o.Str("id"), Name = o.Str("name", o.Str("id")), Branch = o.Str("branch", "logistics"), Tier = o.Int("tier", 1),
                    ResearchPoints = o.Int("researchPoints", 100), IsFinal = o.Bool("final", false),
                };
                var cost = o.Obj("cellCost");
                if (cost != null) foreach (var kv in cost) if (kv.Value is double d) t.CellCost[kv.Key] = (int)d;
                t.Prerequisites = o.StrList("prerequisites");
                t.UnlocksRecipes = o.StrList("unlocksRecipes");
                t.UnlocksBuildings = o.StrList("unlocksBuildings");
                Techs[t.Id] = t;
            }
            var loc = ReadObj(readFile, "loc_en.json");
            if (loc != null) foreach (var kv in loc) if (kv.Value is string s) Loc[kv.Key] = s;

            Validate();
        }

        void LoadBalance(JsonObject o)
        {
            var b = Balance;
            foreach (var f in typeof(Balance).GetFields())
            {
                string key = char.ToLowerInvariant(f.Name[0]) + f.Name.Substring(1);
                if (o.Has(key) && f.FieldType == typeof(int)) f.SetValue(b, o.Int(key));
            }
        }

        void Validate()
        {
            if (Terrains.Count == 0) Problems.Add("no terrain defined");
            foreach (var r in Recipes.Values)
            {
                foreach (var s in r.Inputs) if (!Items.ContainsKey(s.Item)) Problems.Add($"recipe {r.Id}: unknown input item {s.Item}");
                foreach (var s in r.Outputs) if (!Items.ContainsKey(s.Item)) Problems.Add($"recipe {r.Id}: unknown output item {s.Item}");
            }
            foreach (var b in Buildings.Values)
            {
                if (b.Width < 1 || b.Height < 1 || b.Width > 15 || b.Height > 15) Problems.Add($"building {b.Id}: size must be 1..15");
                foreach (var s in b.Cost) if (!Items.ContainsKey(s.Item)) Problems.Add($"building {b.Id}: unknown cost item {s.Item}");
                if (b.Kind == BuildingKind.Crafter && string.IsNullOrEmpty(b.CrafterType)) Problems.Add($"building {b.Id}: crafter needs crafterType");
                if (b.IsConveyor && b.SubPerTick <= 0) Problems.Add($"building {b.Id}: conveyor needs subPerTick");
            }
            foreach (var t in Techs.Values)
            {
                foreach (var p in t.Prerequisites) if (!Techs.ContainsKey(p)) Problems.Add($"tech {t.Id}: unknown prerequisite {p}");
                foreach (var r in t.UnlocksRecipes) if (!Recipes.ContainsKey(r)) Problems.Add($"tech {t.Id}: unknown recipe {r}");
                foreach (var b in t.UnlocksBuildings) if (!Buildings.ContainsKey(b)) Problems.Add($"tech {t.Id}: unknown building {b}");
                foreach (var c in t.CellCost.Keys) if (!Items.ContainsKey(c)) Problems.Add($"tech {t.Id}: unknown cell item {c}");
            }
            foreach (var t in Terrains) if (t.YieldsItem != null && !Items.ContainsKey(t.YieldsItem)) Problems.Add($"terrain {t.Id}: unknown item {t.YieldsItem}");
            foreach (var r in Resources) if (r.Index != 0 && !Items.ContainsKey(r.YieldsItem)) Problems.Add($"resource {r.Id}: unknown item {r.YieldsItem}");
            foreach (var p in Problems) SimLog.Warn("[Content] " + p);
        }

        static JsonObject ReadObj(Func<string, string> readFile, string name)
        {
            string text = readFile(name);
            if (text == null) { SimLog.Warn("[Content] missing " + name); return null; }
            try { return Json.ParseObject(text); }
            catch (Exception e) { SimLog.Error($"[Content] {name}: {e.Message}"); return null; }
        }

        static List<JsonObject> ReadList(Func<string, string> readFile, string name, string key)
        {
            var o = ReadObj(readFile, name);
            return o == null ? new List<JsonObject>() : o.ObjList(key);
        }

        static List<ItemStack> ReadStacks(JsonObject o)
        {
            var list = new List<ItemStack>();
            if (o == null) return list;
            foreach (var kv in o) if (kv.Value is double d && d > 0) list.Add(new ItemStack(kv.Key, (int)d));
            return list;
        }

        public ItemDef Item(string id) => id != null && Items.TryGetValue(id, out var v) ? v : null;
        public BuildingDef Building(string id) => id != null && Buildings.TryGetValue(id, out var v) ? v : null;
        public RecipeDef Recipe(string id) => id != null && Recipes.TryGetValue(id, out var v) ? v : null;
        public TechDef Tech(string id) => id != null && Techs.TryGetValue(id, out var v) ? v : null;
        public TerrainDef Terrain(byte index) => index < Terrains.Count ? Terrains[index] : Terrains[0];
        public TerrainDef TerrainById(string id) => Terrains.Find(t => t.Id == id);
        public ResourceDef Resource(byte index) => index < Resources.Count ? Resources[index] : Resources[0];
        public ResourceDef ResourceById(string id) => Resources.Find(r => r.Id == id);
        public int StackSize(string item) => Item(item)?.StackSize ?? 100;

        /// <summary>Display text: localisation table first, then the def's own name.</summary>
        public string Text(string key, string fallback) => Loc.TryGetValue(key, out var s) ? s : fallback;
    }
}
