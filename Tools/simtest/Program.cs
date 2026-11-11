using System;
using NewGen.Sim.Core;

namespace NewGen.SimTest
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string repoRoot = FindRepoRoot();
            CoreTests.Run();
            // Later test groups are added here as systems are built.
            RunAll(repoRoot);
            Console.WriteLine($"\n{Check.Passed} checks passed, {Check.Failures} failed.");
            return Check.Failures == 0 ? 0 : 1;
        }

        static void RunAll(string repoRoot) { foreach (var t in Extra.Groups) t(repoRoot); }

        static string FindRepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir, "CLAUDE.md")))
                dir = System.IO.Path.GetDirectoryName(dir);
            return dir ?? ".";
        }
    }

    /// <summary>Later test groups register here.</summary>
    public static class Extra { public static readonly System.Collections.Generic.List<Action<string>> Groups = new System.Collections.Generic.List<Action<string>>(); }

    public static class CoreTests
    {
        public static void Run()
        {
            Check.Section("Fix");
            Check.Eq(-1, Fix.TileOf(-1), "floor of negative sub pos");
            Check.Eq(0, Fix.TileOf(255), "sub 255 is tile 0");
            Check.Eq(1, Fix.TileOf(256), "sub 256 is tile 1");
            Check.Eq(128, Fix.Center(0), "center of tile 0");
            Check.Eq(5, Fix.Sqrt(25), "sqrt 25");
            Check.Eq(4, Fix.Sqrt(24), "sqrt 24 floors");
            Fix.Normalize(3, 4, 100, out int nx, out int ny);
            Check.Eq(60, nx, "normalize x"); Check.Eq(80, ny, "normalize y");
            Check.Eq(new TilePos(1, 0), Dir.East.Rotate(new TilePos(0, 1)), "rotate north-offset to east");
            Check.Eq(new TilePos(0, -1), Dir.South.Rotate(new TilePos(0, 1)), "rotate north-offset to south");
            Check.Eq(Dir.West, Dir.East.Opposite(), "opposite");
            Check.Eq(Dir.North, Dir.West.RotateCW(), "rotate cw wraps");

            Check.Section("Rng");
            var a = new Rng(42); var b = new Rng(42);
            bool same = true;
            for (int i = 0; i < 1000; i++) if (a.NextUInt() != b.NextUInt()) same = false;
            Check.True(same, "same seed gives same sequence");
            var c = new Rng(43);
            Check.True(new Rng(42).NextUInt() != c.NextUInt(), "different seeds differ");
            int inRange = 0; var r = new Rng(7);
            for (int i = 0; i < 1000; i++) { int v = r.Range(3, 8); if (v >= 3 && v < 8) inRange++; }
            Check.Eq(1000, inRange, "Range stays in bounds");

            Check.Section("Hash");
            Check.Eq(Hash.At(1, 5, 6), Hash.At(1, 5, 6), "hash deterministic");
            Check.True(Hash.At(1, 5, 6) != Hash.At(1, 6, 5), "hash depends on order");
            Check.Eq(-1, Hash.FloorDiv(-1, 32), "floordiv negative");
            Check.Eq(31, Hash.FloorMod(-1, 32), "floormod negative");
            int n = Hash.ValueNoise16(9, 100, 100, 16, 0);
            Check.True(n >= 0 && n < 65536, "noise in range");
            int n2 = Hash.Fbm16(9, -1000, 777, 64, 4, 3);
            Check.True(n2 >= 0 && n2 < 65536, "fbm in range");
            // continuity: neighbours should not differ wildly
            int d = Math.Abs(Hash.ValueNoise16(9, 100, 100, 32, 0) - Hash.ValueNoise16(9, 101, 100, 32, 0));
            Check.True(d < 8000, "noise is smooth between neighbours");

            Check.Section("Json");
            var o = Json.ParseObject("{\"a\": 1, \"b\": [1, 2.5, \"x\", true, null], \"c\": {\"d\": \"e\\n\"}, // comment\n \"f\": -3e2}");
            Check.Eq(1, o.Int("a"), "int");
            Check.Eq(5, o.Arr("b").Count, "array count");
            Check.Eq("e\n", o.Obj("c").Str("d"), "nested string with escape");
            Check.Eq(-300, o.Int("f"), "exponent number");
            Check.Eq(true, o.Arr("b")[3] is bool bb && bb, "bool in array");
            string round = Json.Write(o, false);
            var o2 = Json.ParseObject(round);
            Check.Eq(Json.Write(o, false), Json.Write(o2, false), "write/parse round trip");
            Check.Throws(() => Json.Parse("{\"a\": }"), "bad json throws");
            Check.Throws(() => Json.Parse("[1,2"), "unterminated array throws");
        }
    }
}
