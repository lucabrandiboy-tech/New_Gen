using NewGen.Sim.Content;
using NewGen.Sim.Entities;
using NewGen.Sim.Core;

namespace NewGen.Sim
{
    // Temporary: replaced by real implementations in later partial files.
    public sealed partial class Simulation
    {
        private partial Building CreateMachine(BuildingDef def) => null;
        private partial Building CreateLogistic(BuildingDef def) => null;
        private partial bool IsPlayerOn(TilePos p) => false;
    }
}
