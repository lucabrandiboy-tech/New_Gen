using System;

namespace NewGen.Sim.Core
{
    /// <summary>The sim has no Unity reference, so logging goes through these hooks. Unity wires them to Debug.Log.</summary>
    public static class SimLog
    {
        public static Action<string> Info = s => Console.WriteLine(s);
        public static Action<string> Warn = s => Console.WriteLine("WARN " + s);
        public static Action<string> Error = s => Console.WriteLine("ERROR " + s);
    }
}
