using System;
using System.Collections.Generic;

namespace NewGen.SimTest
{
    /// <summary>Minimal assertion helper so the harness has no external packages.</summary>
    public static class Check
    {
        public static int Failures;
        public static int Passed;
        static string current = "";

        public static void Section(string name) { current = name; Console.WriteLine("== " + name); }

        public static void True(bool cond, string what)
        {
            if (cond) { Passed++; return; }
            Failures++;
            Console.WriteLine($"  FAIL [{current}] {what}");
        }

        public static void Eq<T>(T expected, T actual, string what)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) { Passed++; return; }
            Failures++;
            Console.WriteLine($"  FAIL [{current}] {what}: expected {expected}, got {actual}");
        }

        public static void Throws(Action a, string what)
        {
            try { a(); } catch { Passed++; return; }
            Failures++;
            Console.WriteLine($"  FAIL [{current}] {what}: expected an exception");
        }
    }
}
