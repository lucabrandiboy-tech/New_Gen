using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>Produces power. The crash pod's reactor decays towards DecayFloorPermille over DecayTicks.</summary>
    public sealed class GeneratorBuilding : Building
    {
        public long TicksAlive;

        public int CurrentOutput()
        {
            if (IsDestroyed) return 0;
            if (Def.DecayTicks <= 0) return Def.PowerOutput;
            long t = TicksAlive >= Def.DecayTicks ? Def.DecayTicks : TicksAlive;
            int permille = 1000 - (int)((1000 - Def.DecayFloorPermille) * t / Def.DecayTicks);
            return Fix.Permille(Def.PowerOutput, permille);
        }

        public override void Tick(Simulation sim)
        {
            TicksAlive++;
            WorkedThisTick = CurrentOutput() > 0;
        }
    }
}
