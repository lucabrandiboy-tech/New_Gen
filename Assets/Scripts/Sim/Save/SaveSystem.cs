using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;
using NewGen.Sim.World;

namespace NewGen.Sim.Save
{
    /// <summary>
    /// Compact binary saves. Terrain is never stored: only chunk deltas, entities, players and research.
    /// File IO (slots, paths) lives in the Unity layer; this class only turns a Simulation into bytes and back.
    /// </summary>
    public static class SaveSystem
    {
        const uint Magic = 0x5653474E; // "NGSV"
        public const int Version = 1;

        // ------------------------------------------------------------------ rules
        public static bool CanManualSave(Simulation sim, out int secondsLeft)
        {
            int cooldown = sim.Settings.Ironman ? sim.Balance.IronmanSaveCooldownSeconds : sim.Balance.ManualSaveCooldownSeconds;
            long since = sim.Tick - sim.LastManualSaveTick;
            if (sim.LastManualSaveTick < 0) { secondsLeft = 0; return true; }
            long left = cooldown - since / sim.TicksPerSecond;
            secondsLeft = (int)Fix.Max(0, (int)left);
            return left <= 0;
        }

        /// <summary>Autosaves are forbidden in ironman.</summary>
        public static bool AutosaveAllowed(Simulation sim) => !sim.Settings.Ironman;

        // ------------------------------------------------------------------ write
        public static byte[] Write(Simulation sim, bool manual)
        {
            if (manual) sim.LastManualSaveTick = sim.Tick;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(Magic); w.Write(Version);
                w.Write(sim.Seed); w.Write(sim.Tick); w.Write(sim.LastManualSaveTick);
                w.Write(sim.Settings.Ironman); w.Write(sim.Settings.Difficulty ?? "normal");
                var rs = sim.Rng.GetState(); foreach (var u in rs) w.Write(u);
                w.Write(sim.NextEntityId);

                // research
                w.Write(sim.Research.Queue.Count); foreach (var t in sim.Research.Queue) w.Write(t);
                w.Write(sim.Research.Unlocked.Count); foreach (var t in sim.Research.Unlocked) w.Write(t);
                w.Write(sim.Research.ProgressPermille); w.Write(sim.Research.FinalDone);
                w.Write(sim.UnlockedBuildings.Count); foreach (var b in sim.UnlockedBuildings) w.Write(b);
                w.Write(sim.UnlockedRecipes.Count); foreach (var r in sim.UnlockedRecipes) w.Write(r);

                // chunk deltas
                var modified = new List<Chunk>();
                foreach (var c in sim.World.LoadedChunks) if (c.TilesModified) modified.Add(c);
                int written = 0; long countPos = ms.Position; w.Write(0);
                foreach (var c in modified)
                {
                    var delta = sim.World.Delta(c);
                    if (delta.Count == 0) continue;
                    w.Write(c.CX); w.Write(c.CY); w.Write(delta.Count);
                    foreach (var kv in delta) { w.Write((ushort)kv.Key); w.Write(kv.Value.Terrain); w.Write(kv.Value.Resource); }
                    written++;
                }
                long end = ms.Position; ms.Position = countPos; w.Write(written); ms.Position = end;

                // buildings
                w.Write(sim.Buildings.Count);
                foreach (var b in sim.Buildings) WriteBuilding(w, b);

                // players
                w.Write(sim.Players.Count);
                foreach (var p in sim.Players) WritePlayer(w, p);

                w.Flush();
                return ms.ToArray();
            }
        }

        static void WriteInventory(BinaryWriter w, Inventory inv)
        {
            if (inv == null) { w.Write(-1); return; }
            w.Write(inv.SlotCount);
            for (int i = 0; i < inv.SlotCount; i++)
            {
                var s = inv.Slots[i];
                w.Write(s?.Item ?? ""); w.Write(s?.Count ?? 0); w.Write(inv.Filters[i] ?? "");
            }
        }

        static void WriteBuilding(BinaryWriter w, Building b)
        {
            w.Write(b.Id); w.Write(b.Def.Id); w.Write(b.Origin.X); w.Write(b.Origin.Y); w.Write((byte)b.Facing);
            w.Write(b.Health); w.Write((byte)b.State);
            switch (b)
            {
                case GeneratorBuilding g: w.Write(g.TicksAlive); break;
                case DrillBuilding d: w.Write(d.Progress); w.Write(d.Buffered ?? ""); w.Write(d.NextTileIndex); break;
                case CrafterBuilding c: w.Write(c.RecipeId ?? ""); w.Write(c.Progress); WriteInventory(w, c.Inputs); WriteInventory(w, c.Outputs); break;
                case DataCenterBuilding dc: WriteInventory(w, dc.Cells); w.Write(dc.Heat); w.Write(dc.Overheated); break;
                case ChestBuilding ch: WriteInventory(w, ch.Storage); break;
                case ConveyorBuilding cv:
                    w.Write(cv.Items.Count); foreach (var it in cv.Items) { w.Write(it.Item); w.Write(it.Pos); }
                    w.Write(cv.FeedFrom.HasValue); w.Write((byte)(cv.FeedFrom ?? Dir.North)); break;
                case ArmBuilding a: w.Write(a.Holding ?? ""); w.Write(a.SwingProgress); w.Write(a.SwingingForward); break;
            }
        }

        static void WritePlayer(BinaryWriter w, Player p)
        {
            w.Write(p.Id); w.Write(p.Pos.X); w.Write(p.Pos.Y); w.Write(p.Health); w.Write(p.Temperature); w.Write((byte)p.Facing); w.Write(p.Dead);
            WriteInventory(w, p.Inventory);
            w.Write(p.CraftQueue.Count);
            foreach (var j in p.CraftQueue) { w.Write(j.RecipeId); w.Write(j.Progress); w.Write(j.Started); }
            w.Write(p.QuickbarPage);
        }

        // ------------------------------------------------------------------ read
        public static Simulation Read(byte[] data, ContentDatabase db)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8))
            {
                if (r.ReadUInt32() != Magic) throw new InvalidDataException("not a New Generation save");
                int ver = r.ReadInt32();
                if (ver > Version) throw new InvalidDataException("save is from a newer version");
                ulong seed = r.ReadUInt64();
                var sim = new Simulation(db, seed);
                sim.SetTick(r.ReadInt64()); sim.LastManualSaveTick = r.ReadInt64();
                sim.Settings.Ironman = r.ReadBoolean(); sim.Settings.Difficulty = r.ReadString();
                var rs = new uint[4]; for (int i = 0; i < 4; i++) rs[i] = r.ReadUInt32(); sim.Rng.SetState(rs);
                int nextId = r.ReadInt32();

                int n = r.ReadInt32(); for (int i = 0; i < n; i++) sim.Research.Queue.Add(r.ReadString());
                n = r.ReadInt32(); for (int i = 0; i < n; i++) sim.Research.Unlocked.Add(r.ReadString());
                sim.Research.ProgressPermille = r.ReadInt32(); sim.Research.FinalDone = r.ReadBoolean();
                n = r.ReadInt32(); for (int i = 0; i < n; i++) sim.UnlockedBuildings.Add(r.ReadString());
                n = r.ReadInt32(); for (int i = 0; i < n; i++) sim.UnlockedRecipes.Add(r.ReadString());

                int chunks = r.ReadInt32();
                for (int i = 0; i < chunks; i++)
                {
                    int cx = r.ReadInt32(), cy = r.ReadInt32(), cnt = r.ReadInt32();
                    var delta = new List<KeyValuePair<int, Tile>>(cnt);
                    for (int k = 0; k < cnt; k++) { int idx = r.ReadUInt16(); byte t = r.ReadByte(), res = r.ReadByte(); delta.Add(new KeyValuePair<int, Tile>(idx, new Tile(t, res))); }
                    sim.World.ApplyDelta(cx, cy, delta);
                }

                int bcount = r.ReadInt32();
                for (int i = 0; i < bcount; i++) ReadBuilding(r, sim);

                int pcount = r.ReadInt32();
                for (int i = 0; i < pcount; i++) ReadPlayer(r, sim);

                sim.SetNextEntityId(nextId);
                sim.MarkNetworksDirty();
                return sim;
            }
        }

        static Inventory ReadInventory(BinaryReader r, ContentDatabase db, Inventory into)
        {
            int slots = r.ReadInt32();
            if (slots < 0) return null;
            var inv = into ?? new Inventory(db, slots);
            for (int i = 0; i < slots; i++)
            {
                string item = r.ReadString(); int count = r.ReadInt32(); string filter = r.ReadString();
                if (i < inv.SlotCount)
                {
                    inv.Slots[i] = item.Length > 0 && count > 0 ? new ItemStack(item, count) : null;
                    inv.Filters[i] = filter.Length > 0 ? filter : null;
                }
            }
            return inv;
        }

        static void ReadBuilding(BinaryReader r, Simulation sim)
        {
            int id = r.ReadInt32(); string defId = r.ReadString();
            var origin = new TilePos(r.ReadInt32(), r.ReadInt32()); var facing = (Dir)r.ReadByte();
            int health = r.ReadInt32(); var state = (BuildingState)r.ReadByte();
            var b = sim.RestoreBuilding(id, defId, origin, facing, health, state);
            // b may be null if the def no longer exists; we still have to consume the bytes.
            switch (b)
            {
                case GeneratorBuilding g: g.TicksAlive = r.ReadInt64(); break;
                case DrillBuilding d: d.Progress = r.ReadInt32(); d.Buffered = NullIfEmpty(r.ReadString()); d.NextTileIndex = r.ReadInt32(); break;
                case CrafterBuilding c: c.RecipeId = NullIfEmpty(r.ReadString()); c.Progress = r.ReadInt32(); ReadInventory(r, sim.Db, c.Inputs); ReadInventory(r, sim.Db, c.Outputs); break;
                case DataCenterBuilding dc: ReadInventory(r, sim.Db, dc.Cells); dc.Heat = r.ReadInt32(); dc.Overheated = r.ReadBoolean(); break;
                case ChestBuilding ch: ReadInventory(r, sim.Db, ch.Storage); break;
                case ConveyorBuilding cv:
                    {
                        int n = r.ReadInt32();
                        for (int i = 0; i < n; i++) cv.Items.Add(new ConveyorItem { Item = r.ReadString(), Pos = r.ReadInt32() });
                        bool has = r.ReadBoolean(); var d = (Dir)r.ReadByte(); cv.FeedFrom = has ? d : (Dir?)null; break;
                    }
                case ArmBuilding a: a.Holding = NullIfEmpty(r.ReadString()); a.SwingProgress = r.ReadInt32(); a.SwingingForward = r.ReadBoolean(); break;
                case null:
                    {
                        // unknown building kind: skip by re-reading using a dummy based on the def kind is impossible; abort cleanly
                        throw new InvalidDataException("save references unknown building '" + defId + "'");
                    }
            }
        }

        static void ReadPlayer(BinaryReader r, Simulation sim)
        {
            int id = r.ReadInt32();
            var pos = new SubPos(r.ReadInt32(), r.ReadInt32());
            var p = sim.RestorePlayer(id, pos);
            p.Health = r.ReadInt32(); p.Temperature = r.ReadInt32(); p.Facing = (Dir)r.ReadByte(); p.Dead = r.ReadBoolean();
            ReadInventory(r, sim.Db, p.Inventory);
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++) p.CraftQueue.Add(new CraftJob { RecipeId = r.ReadString(), Progress = r.ReadInt32(), Started = r.ReadBoolean() });
            p.QuickbarPage = r.ReadInt32();
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

        // ------------------------------------------------------------------ determinism check
        /// <summary>Cheap hash of the important state. Two sims that ran the same inputs must match (future desync detector).</summary>
        public static uint StateHash(Simulation sim)
        {
            uint h = 2166136261;
            void Mix(long v) { unchecked { h ^= (uint)v; h *= 16777619; h ^= (uint)(v >> 32); h *= 16777619; } }
            void MixInv(Inventory inv)
            {
                if (inv == null) return;
                foreach (var st in inv.Slots) { if (st == null) { Mix(0); continue; } Mix(st.Count); Mix(StableHash(st.Item)); }
            }
            Mix(sim.Tick);
            foreach (var b in sim.Buildings)
            {
                Mix(b.Id); Mix(b.Origin.X); Mix(b.Origin.Y); Mix(b.Health); Mix((int)b.State); Mix(b.PowerSatisfaction);
                if (b is ConveyorBuilding cv) foreach (var it in cv.Items) { Mix(it.Pos); Mix(StableHash(it.Item)); }
                if (b is CrafterBuilding c) { Mix(c.Progress); MixInv(c.Inputs); MixInv(c.Outputs); }
                if (b is ChestBuilding ch) MixInv(ch.Storage);
                if (b is DrillBuilding d) Mix(d.Progress);
                if (b is DataCenterBuilding dc) Mix(dc.Heat);
                if (b is ArmBuilding a) Mix(a.SwingProgress);
            }
            foreach (var p in sim.Players) { Mix(p.Pos.X); Mix(p.Pos.Y); Mix(p.Health); Mix(p.Temperature); MixInv(p.Inventory); }
            Mix(sim.Research.ProgressPermille);
            return h;
        }

        /// <summary>string.GetHashCode is randomised per process; this is not.</summary>
        static int StableHash(string s)
        {
            unchecked { int h = 23; foreach (char c in s) h = h * 31 + c; return h; }
        }
    }
}
