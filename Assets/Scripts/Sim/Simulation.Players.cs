using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.Sim
{
    public sealed class GameSettings
    {
        public bool Ironman;
        public string Difficulty = "normal";   // normal, hard, brutal
    }

    public sealed partial class Simulation
    {
        public readonly List<Player> Players = new List<Player>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public GameSettings Settings = new GameSettings();
        readonly List<Projectile> projectileScratch = new List<Projectile>();

        public Player LocalPlayer => Players.Count > 0 ? Players[0] : null;

        public Player SpawnPlayer(SubPos at)
        {
            var p = new Player
            {
                Pos = at,
                Health = Balance.PlayerMaxHealth,
                Temperature = Balance.PlayerMaxTemperature,
                Inventory = new Inventory(Db, Balance.PlayerInventorySlots),
            };
            Register(p);
            Players.Add(p);
            return p;
        }

        /// <summary>New game: crash pod at the origin, player next to it.</summary>
        public void StartNewGame()
        {
            Place("crashpod", new TilePos(-1, -1), Dir.North);
            SpawnPlayer(SubPos.FromTileCenter(new TilePos(0, -3)));
        }

        private partial bool IsPlayerOn(TilePos p)
        {
            foreach (var pl in Players)
            {
                if (pl.Dead) continue;
                int r = Player.Radius;
                int minX = Fix.TileOf(pl.Pos.X - r), maxX = Fix.TileOf(pl.Pos.X + r);
                int minY = Fix.TileOf(pl.Pos.Y - r), maxY = Fix.TileOf(pl.Pos.Y + r);
                if (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY) return true;
            }
            return false;
        }

        partial void TickPlayers()
        {
            foreach (var p in Players) if (!p.Dead) TickPlayer(p);
        }

        void TickPlayer(Player p)
        {
            ref PlayerInput inp = ref p.Input;

            // --- movement
            int speed = Balance.PlayerWalkSubPerTick;
            var terrain = World.TerrainAt(p.Tile);
            speed = Fix.Permille(speed, terrain.WalkSpeedPermille);
            Fix.Normalize(inp.MoveX, inp.MoveY, speed, out int vx, out int vy);
            if (Fix.Abs(inp.MoveX) < 100 && Fix.Abs(inp.MoveY) < 100) { vx = 0; vy = 0; }
            p.Moving = vx != 0 || vy != 0;
            if (p.Moving)
            {
                TryMove(p, vx, 0);
                TryMove(p, 0, vy);
                p.Facing = DirExt.FromDelta(vx, vy);
            }
            else
            {
                // face the cursor when standing still
                int dx = inp.Aim.X - p.Pos.X, dy = inp.Aim.Y - p.Pos.Y;
                if (Fix.Abs(dx) + Fix.Abs(dy) > 64) p.Facing = DirExt.FromDelta(dx, dy);
            }

            // --- temperature
            int cold = Balance.PlayerBaseColdPerTick + terrain.ColdPerTick;
            int warmth = WarmthAt(p.Pos);
            p.Temperature = Fix.Clamp(p.Temperature - cold + warmth, 0, Balance.PlayerMaxTemperature);
            if (p.Temperature <= 0) { Kill(p, "death.body"); return; }

            // --- hand mining
            p.Mining = false;
            if (inp.Mine)
            {
                var target = inp.Aim.Tile;
                if (InReach(p, target) && TryHandMine(p, target)) p.Mining = true;
            }
            if (!p.Mining) p.MineProgress = 0;

            // --- hand crafting
            TickCrafting(p);

            // --- shooting
            if (p.ShootCooldown > 0) p.ShootCooldown--;
            if (inp.Fire && p.ShootCooldown == 0)
            {
                Shoot(p, inp.Aim);
                p.ShootCooldown = Balance.PlayerShootTicks;
            }
        }

        void TryMove(Player p, int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;
            var np = new SubPos(p.Pos.X + dx, p.Pos.Y + dy);
            if (CanStand(np)) { p.Pos = np; return; }
            // try smaller steps so we can slide right up to walls
            int half = dx / 2, halfY = dy / 2;
            if ((half != 0 || halfY != 0) && CanStand(new SubPos(p.Pos.X + half, p.Pos.Y + halfY)))
                p.Pos = new SubPos(p.Pos.X + half, p.Pos.Y + halfY);
        }

        public bool CanStand(SubPos pos)
        {
            int r = Player.Radius;
            for (int cy = -1; cy <= 1; cy += 2)
                for (int cx = -1; cx <= 1; cx += 2)
                {
                    var t = new SubPos(pos.X + cx * r, pos.Y + cy * r).Tile;
                    if (!World.TerrainAt(t).Walkable) return false;
                    var b = BuildingAt(t);
                    if (b != null && !b.Def.IsConveyor) return false;   // you can walk over tubes/wires
                }
            return true;
        }

        public bool InReach(Player p, TilePos t)
        {
            var c = SubPos.FromTileCenter(t);
            return p.Pos.DistanceTo(c) <= Balance.PlayerReach * Fix.SubPerTile;
        }

        int WarmthAt(SubPos pos)
        {
            int total = 0;
            foreach (var b in buildings)
            {
                if (b.Def.WarmthPerTick == 0 || b.IsDestroyed) continue;
                if (b.Def.PowerUse > 0 && !b.WorkedThisTick && b.State != BuildingState.Working) continue;   // machines warm only while running
                int d = b.Pos.DistanceTo(pos);
                if (d <= (b.Def.WarmthRadiusTiles + Fix.Max(b.Width, b.Height) / 2) * Fix.SubPerTile) total += b.Def.WarmthPerTick;
            }
            return total;
        }

        bool TryHandMine(Player p, TilePos t)
        {
            if (BuildingAt(t) != null) return false;
            var tile = World.GetTile(t);
            string item; int ticks;
            if (tile.Resource != 0) { var r = Db.Resource(tile.Resource); item = r.YieldsItem; ticks = r.HandMineTicks; }
            else { var td = Db.Terrain(tile.Terrain); if (td.YieldsItem == null) return false; item = td.YieldsItem; ticks = td.HandMineTicks; }
            if (!p.Inventory.CanAdd(item, 1)) return false;
            if (p.MineTarget != t) { p.MineTarget = t; p.MineProgress = 0; }
            p.MineProgress += Balance.PlayerHandMineSpeedPermille;
            if (p.MineProgress >= ticks * 1000)
            {
                p.MineProgress -= ticks * 1000;
                p.Inventory.Add(item, 1); p.Version++;
                Events.Add(new SimEvent { Type = SimEventType.ItemPickedUp, EntityId = p.Id, Text = item, Value = 1, Tile = t });
            }
            return true;
        }

        public int MineProgressPermille(Player p)
        {
            if (!p.Mining) return 0;
            var tile = World.GetTile(p.MineTarget);
            int ticks = tile.Resource != 0 ? Db.Resource(tile.Resource).HandMineTicks : Db.Terrain(tile.Terrain).HandMineTicks;
            return ticks == 0 ? 0 : p.MineProgress / ticks;
        }

        // --- hand crafting
        public bool CanHandCraft(Player p, string recipeId, out string reason)
        {
            reason = null;
            var r = Db.Recipe(recipeId);
            if (r == null || r.Crafter != "hand") { reason = "not a hand recipe"; return false; }
            if (!Research.IsRecipeUnlocked(r, this)) { reason = "locked"; return false; }
            // count what is already reserved by queued-but-not-started jobs
            var need = new Dictionary<string, int>();
            foreach (var s in r.Inputs) need[s.Item] = s.Count;
            foreach (var job in p.CraftQueue)
                if (!job.Started) foreach (var s in Db.Recipe(job.RecipeId).Inputs) { need.TryGetValue(s.Item, out int c); need[s.Item] = c + s.Count; }
            foreach (var kv in need) if (!p.Inventory.Has(kv.Key, kv.Value)) { reason = "missing " + kv.Key; return false; }
            return true;
        }

        public bool QueueHandCraft(Player p, string recipeId, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (!CanHandCraft(p, recipeId, out _)) return i > 0;
                p.CraftQueue.Add(new CraftJob { RecipeId = recipeId });
            }
            p.Version++;
            return true;
        }

        public void CancelHandCraft(Player p, int index)
        {
            if (index < 0 || index >= p.CraftQueue.Count) return;
            var job = p.CraftQueue[index];
            if (job.Started) foreach (var s in Db.Recipe(job.RecipeId).Inputs) p.Inventory.Add(s.Item, s.Count);
            p.CraftQueue.RemoveAt(index); p.Version++;
        }

        void TickCrafting(Player p)
        {
            if (p.CraftQueue.Count == 0) return;
            var job = p.CraftQueue[0];
            var r = Db.Recipe(job.RecipeId);
            if (r == null) { p.CraftQueue.RemoveAt(0); return; }
            if (!job.Started)
            {
                if (!p.Inventory.RemoveAll(r.Inputs)) { p.CraftQueue.RemoveAt(0); p.Version++; return; }
                job.Started = true;
            }
            job.Progress += 1000;
            if (job.Progress < r.Ticks * 1000) return;
            foreach (var o in r.Outputs) p.Inventory.Add(o.Item, o.Count);   // overflow lost until ground items exist
            p.CraftQueue.RemoveAt(0); p.Version++;
        }

        // --- shooting
        void Shoot(Player p, SubPos aim)
        {
            int dx = aim.X - p.Pos.X, dy = aim.Y - p.Pos.Y;
            if (dx == 0 && dy == 0) dy = -1;
            Fix.Normalize(dx, dy, Balance.ProjectileSpeedSubPerTick, out int vx, out int vy);
            var pr = new Projectile { Pos = p.Pos, VelX = vx, VelY = vy, RangeLeft = Balance.ProjectileRangeTiles * Fix.SubPerTile, Damage = Balance.ProjectileDamage, OwnerId = p.Id };
            Register(pr);
            Projectiles.Add(pr);
            Events.Add(new SimEvent { Type = SimEventType.Shot, EntityId = pr.Id, Tile = p.Tile });
        }

        partial void TickProjectiles()
        {
            projectileScratch.Clear(); projectileScratch.AddRange(Projectiles);
            foreach (var pr in projectileScratch)
            {
                // step in sub-steps so fast bullets cannot skip a tile
                int steps = Fix.Max(1, (Fix.Abs(pr.VelX) + Fix.Abs(pr.VelY)) / 128 + 1);
                bool done = false;
                for (int s = 0; s < steps && !done; s++)
                {
                    pr.Pos = new SubPos(pr.Pos.X + pr.VelX / steps, pr.Pos.Y + pr.VelY / steps);
                    pr.RangeLeft -= (Fix.Abs(pr.VelX) + Fix.Abs(pr.VelY)) / steps;
                    var b = BuildingAt(pr.Tile);
                    if (b != null && !b.IsDestroyed && !b.Def.IsConveyor)
                    {
                        Damage(b, pr.Damage);
                        Events.Add(new SimEvent { Type = SimEventType.Hit, EntityId = b.Id, Tile = pr.Tile, Value = pr.Damage });
                        done = true;
                    }
                    else if (pr.RangeLeft <= 0 || !World.TerrainAt(pr.Tile).Walkable && World.TerrainAt(pr.Tile).Id == "ocean") done = pr.RangeLeft <= 0;
                }
                if (done || pr.RangeLeft <= 0) { Projectiles.Remove(pr); Unregister(pr); }
            }
        }

        public void HurtPlayer(Player p, int amount, string cause)
        {
            if (p.Dead) return;
            p.Health = Fix.Max(0, p.Health - amount); p.Version++;
            if (p.Health == 0) Kill(p, cause);
        }

        void Kill(Player p, string cause)
        {
            p.Dead = true; p.Version++;
            Events.Add(new SimEvent { Type = SimEventType.PlayerDied, EntityId = p.Id, Text = cause });
        }

        // --- chunk streaming
        partial void AfterTick()
        {
            if (Tick % 60 != 0) return;
            int keep = Balance.ChunkLoadRadius + 2;
            var toUnload = new List<(int, int)>();
            foreach (var c in World.LoadedChunks)
            {
                bool near = false;
                foreach (var p in Players)
                {
                    int pcx = NewGen.Sim.World.Chunk.ChunkOf(p.Tile.X), pcy = NewGen.Sim.World.Chunk.ChunkOf(p.Tile.Y);
                    if (Fix.Abs(c.CX - pcx) <= keep && Fix.Abs(c.CY - pcy) <= keep) { near = true; break; }
                }
                if (near || c.TilesModified) continue;
                bool hasBuilding = false;
                foreach (int occ in c.Occupant) if (occ != 0) { hasBuilding = true; break; }
                if (!hasBuilding) toUnload.Add((c.CX, c.CY));
            }
            foreach (var (cx, cy) in toUnload) World.Unload(cx, cy);
        }
    }
}
