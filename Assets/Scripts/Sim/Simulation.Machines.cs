using NewGen.Sim.Content;
using NewGen.Sim.Entities;

namespace NewGen.Sim
{
    public sealed partial class Simulation
    {
        private partial Building CreateMachine(BuildingDef def)
        {
            switch (def.Kind)
            {
                case BuildingKind.Generator: return new GeneratorBuilding();
                default: return null;
            }
        }
    }
}
