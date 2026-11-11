using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Entities;

namespace NewGen.Sim.Systems
{
    /// <summary>Tech queue (max 10), progress of the head tech, unlocked techs.</summary>
    public sealed class ResearchState
    {
        readonly ContentDatabase db; readonly int queueMax;
        public readonly List<string> Queue = new List<string>();
        public readonly HashSet<string> Unlocked = new HashSet<string>();
        /// <summary>Points earned so far on the head tech (in permille of a point).</summary>
        public int ProgressPermille;
        /// <summary>Cells owed per cell type (permille): consumed when >= 1000.</summary>
        readonly Dictionary<string, int> owed = new Dictionary<string, int>();
        public bool FinalDone;
        public int Version;

        public ResearchState(ContentDatabase db, int queueMax) { this.db = db; this.queueMax = queueMax; }

        public TechDef Current => Queue.Count == 0 ? null : db.Tech(Queue[0]);

        public bool CanQueue(string techId, out string reason)
        {
            reason = null;
            var t = db.Tech(techId);
            if (t == null) { reason = "unknown tech"; return false; }
            if (Unlocked.Contains(techId)) { reason = "already researched"; return false; }
            if (Queue.Contains(techId)) { reason = "already queued"; return false; }
            if (Queue.Count >= queueMax) { reason = "queue full"; return false; }
            foreach (var p in t.Prerequisites)
                if (!Unlocked.Contains(p) && !Queue.Contains(p)) { reason = "needs " + p; return false; }
            return true;
        }

        public bool Enqueue(string techId)
        {
            if (!CanQueue(techId, out _)) return false;
            Queue.Add(techId); Version++;
            return true;
        }

        public void Dequeue(string techId)
        {
            int i = Queue.IndexOf(techId);
            if (i < 0) return;
            // removing a tech also removes anything queued after it that depends on it
            Queue.RemoveAt(i);
            for (int j = Queue.Count - 1; j >= i; j--)
            {
                var t = db.Tech(Queue[j]);
                foreach (var p in t.Prerequisites)
                    if (!Unlocked.Contains(p) && !Queue.Contains(p)) { Queue.RemoveAt(j); break; }
            }
            if (i == 0) { ProgressPermille = 0; owed.Clear(); }
            Version++;
        }

        /// <summary>A data center offers one tick of work. Returns false if it could not pay the cells.</summary>
        public bool TryContribute(DataCenterBuilding dc, Simulation sim)
        {
            var t = Current;
            if (t == null) return false;
            int pointsPermille = dc.Def.ResearchPerTick * dc.PowerSatisfaction;   // 1000 = one point per tick at full power
            // cells owed for these points
            foreach (var kv in t.CellCost)
            {
                int add = (int)((long)kv.Value * pointsPermille / t.ResearchPoints);
                owed.TryGetValue(kv.Key, out int cur);
                owed[kv.Key] = cur + add;
            }
            // pay what is due
            foreach (var kv in t.CellCost)
            {
                int due = owed[kv.Key] / 1000;
                if (due <= 0) continue;
                if (dc.Cells.Count(kv.Key) < due)
                {
                    // undo the owed increase so we do not drift while stalled
                    foreach (var k2 in t.CellCost) owed[k2.Key] -= (int)((long)k2.Value * pointsPermille / t.ResearchPoints);
                    return false;
                }
                dc.Cells.Remove(kv.Key, due);
                owed[kv.Key] -= due * 1000;
            }
            ProgressPermille += pointsPermille;
            return true;
        }

        /// <summary>Called once per tick by the simulation: finish the head tech when done.</summary>
        public void CheckCompletion(Simulation sim)
        {
            var t = Current;
            if (t == null) return;
            if (ProgressPermille < t.ResearchPoints * 1000) return;
            Queue.RemoveAt(0);
            ProgressPermille = 0; owed.Clear();
            Unlocked.Add(t.Id);
            foreach (var b in t.UnlocksBuildings) sim.UnlockedBuildings.Add(b);
            foreach (var r in t.UnlocksRecipes) sim.UnlockedRecipes.Add(r);
            if (t.IsFinal) FinalDone = true;
            Version++;
            sim.Events.Add(new SimEvent { Type = SimEventType.ResearchFinished, Text = t.Id });
        }

        public int ProgressOfCurrent1000()
        {
            var t = Current;
            return t == null ? 0 : (int)((long)ProgressPermille / t.ResearchPoints);
        }

        public bool IsRecipeUnlocked(RecipeDef r, Simulation sim) => r != null && (r.UnlockedAtStart || sim.UnlockedRecipes.Contains(r.Id));
    }
}
