using NewGen.Sim;
using NewGen.Sim.Content;
using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.SimTest
{
    public static class PlayerTests
    {
        static void Steps(Simulation sim, int n) { for (int i = 0; i < n; i++) sim.Step(); }

        public static void Run(string repoRoot)
        {
            Check.Section("Player movement");
            var sim = EntityTests.NewSim();
            sim.StartNewGame();
            var p = sim.LocalPlayer;
            Check.True(p != null && !p.Dead, "player spawned");
            var start = p.Pos;
            p.Input = new PlayerInput { MoveX = 1000, MoveY = 0, Aim = p.Pos };
            Steps(sim, 30);
            int moved = p.Pos.X - start.X;
            Check.True(moved > 1500 && moved < 2300, "walked ~7-8 tiles east in 1 s: " + moved);
            Check.Eq(Dir.East, p.Facing, "faces east");
            Check.True(p.Moving, "moving flag");
            // walk back into the crash pod: blocked
            p.Pos = SubPos.FromTileCenter(new TilePos(0, -3));
            p.Input = new PlayerInput { MoveX = 0, MoveY = 1000, Aim = p.Pos };
            Steps(sim, 60);
            Check.True(p.Tile.Y < -1, "pod blocks the player: y=" + p.Tile.Y);
            Check.True(sim.CanPlace("chest", p.Tile, Dir.North) == PlaceResult.PlayerInWay, "cannot build on the player");
            // can walk over wires
            sim.Place("wire", new TilePos(0, -5), Dir.East);
            p.Pos = SubPos.FromTileCenter(new TilePos(0, -6));
            p.Input = new PlayerInput { MoveY = 1000, Aim = p.Pos };
            Steps(sim, 15);
            Check.True(p.Tile.Y >= -5, "walks over a wire");
            // ocean blocks
            int edge = sim.Balance.TestMapRadiusChunks * 32 + 80;
            int coast = edge; while (coast > 0 && !sim.World.TerrainAt(new TilePos(coast, 0)).Walkable) coast--;
            p.Pos = SubPos.FromTileCenter(new TilePos(coast - 5, 0));
            p.Input = new PlayerInput { MoveX = 1000, Aim = p.Pos };
            Steps(sim, 600);
            Check.True(sim.World.TerrainAt(p.Tile).Walkable, "stops at the coast");

            Check.Section("Temperature");
            var s2 = EntityTests.NewSim(); s2.StartNewGame(); var p2 = s2.LocalPlayer;
            p2.Input = new PlayerInput { Aim = p2.Pos };
            int t0 = p2.Temperature;
            Steps(s2, 100);
            Check.True(p2.Temperature >= t0, "warm next to the pod");
            p2.Pos = SubPos.FromTileCenter(new TilePos(60, 60));
            Steps(s2, 100);
            Check.True(p2.Temperature < t0, "cools away from heat");
            Steps(s2, 5000);
            Check.True(p2.Dead, "freezes to death eventually");
            bool died = false; foreach (var e in s2.Events) if (e.Type == SimEventType.PlayerDied) died = true;
            Check.True(died, "PlayerDied event");

            Check.Section("Hand mining");
            var s3 = EntityTests.NewSim(); s3.StartNewGame(); var p3 = s3.LocalPlayer;
            // starter iron patch is north around (0,16)
            TilePos ore = new TilePos(0, 16);
            Check.Eq(1, (int)s3.World.GetTile(ore).Resource, "starter iron at (0,16)");
            p3.Pos = SubPos.FromTileCenter(new TilePos(0, 13));
            p3.Input = new PlayerInput { Mine = true, Aim = SubPos.FromTileCenter(ore) };
            Steps(sim: s3, n: 61);
            Check.Eq(1, p3.Inventory.Count("iron_ore"), "one ore after 60 ticks");
            Check.True(p3.Mining, "mining flag set");
            p3.Pos = SubPos.FromTileCenter(new TilePos(0, 0));
            Steps(s3, 61);
            Check.Eq(1, p3.Inventory.Count("iron_ore"), "out of reach: no more ore");
            // ice terrain yields ice
            TilePos icePos = new TilePos(int.MinValue, 0);
            for (int y = -40; y < 40 && icePos.X == int.MinValue; y++) for (int x = -40; x < 40; x++) if (s3.World.TerrainAt(new TilePos(x, y)).Id == "ice" && s3.World.GetTile(new TilePos(x, y)).Resource == 0) { icePos = new TilePos(x, y); break; }
            Check.True(icePos.X != int.MinValue, "found ice");
            p3.Pos = SubPos.FromTileCenter(icePos + new TilePos(1, 0));
            p3.Input = new PlayerInput { Mine = true, Aim = SubPos.FromTileCenter(icePos) };
            Steps(s3, 41);
            Check.Eq(1, p3.Inventory.Count("ice"), "mined ice from ice terrain");

            Check.Section("Hand crafting");
            var db = ContentTests.Db;
            var tmp = new RecipeDef { Id = "__hand_test", Name = "t", Crafter = "hand", Ticks = 20 };
            tmp.Inputs.Add(new ItemStack("iron_ore", 2)); tmp.Outputs.Add(new ItemStack("iron_plate", 1));
            db.Recipes[tmp.Id] = tmp;
            try
            {
                var s4 = EntityTests.NewSim(); s4.StartNewGame(); var p4 = s4.LocalPlayer; p4.Input = new PlayerInput { Aim = p4.Pos };
                Check.True(!s4.CanHandCraft(p4, tmp.Id, out string why) && why.StartsWith("missing"), "cannot craft without ore");
                p4.Inventory.Add("iron_ore", 5);
                Check.True(s4.QueueHandCraft(p4, tmp.Id, 2), "queued 2");
                Check.True(!s4.QueueHandCraft(p4, tmp.Id, 1), "third needs 6 ore, only 5");
                Steps(s4, 21);
                Check.Eq(1, p4.Inventory.Count("iron_plate"), "first plate after 20 ticks");
                Steps(s4, 21);
                Check.Eq(2, p4.Inventory.Count("iron_plate"), "second plate");
                Check.Eq(1, p4.Inventory.Count("iron_ore"), "4 ore used");
                s4.QueueHandCraft(p4, tmp.Id, 1);
                Check.Eq(0, p4.CraftQueue.Count, "cannot queue with 1 ore");
                p4.Inventory.Add("iron_ore", 1);
                s4.QueueHandCraft(p4, tmp.Id, 1); Steps(s4, 1);
                s4.CancelHandCraft(p4, 0);
                Check.Eq(2, p4.Inventory.Count("iron_ore"), "cancel refunds consumed inputs");
            }
            finally { db.Recipes.Remove(tmp.Id); }

            Check.Section("Shooting");
            var s5 = EntityTests.NewSim(); s5.StartNewGame(); var p5 = s5.LocalPlayer;
            var chest = s5.Place("chest", new TilePos(6, -3), Dir.North);
            p5.Pos = SubPos.FromTileCenter(new TilePos(0, -3));
            p5.Input = new PlayerInput { Fire = true, Aim = SubPos.FromTileCenter(new TilePos(6, -3)) };
            Steps(s5, 1);
            Check.Eq(1, s5.Projectiles.Count, "projectile spawned");
            Steps(s5, 10);
            Check.True(chest.Health < chest.Def.MaxHealth, "chest took damage");
            Check.Eq(1, s5.Projectiles.Count, "second shot after cooldown, first gone");
            p5.Input = new PlayerInput { Aim = p5.Pos };
            Steps(s5, 40);
            Check.Eq(0, s5.Projectiles.Count, "projectiles expire");
            Steps(s5, 200);
            Check.True(s5.World.LoadedChunkCount < 60, "far chunks unloaded: " + s5.World.LoadedChunkCount);
        }
    }
}
