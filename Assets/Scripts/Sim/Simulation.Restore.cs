using NewGen.Sim.Core;
using NewGen.Sim.Entities;

namespace NewGen.Sim
{
    /// <summary>Hooks used only by SaveSystem to rebuild state without re-running placement rules.</summary>
    public sealed partial class Simulation
    {
        public long LastManualSaveTick = -1;

        internal void SetTick(long t) => Tick = t;

        internal Building RestoreBuilding(int id, string defId, TilePos origin, Dir facing, int health, BuildingState state)
        {
            var def = Db.Building(defId);
            if (def == null) return null;
            var b = CreateBuilding(def);
            b.Def = def; b.Origin = origin; b.Facing = facing; b.Health = health; b.State = state;
            b.Pos = Building.CenterOf(origin, b.Width, b.Height);
            RegisterWithId(b, id);
            foreach (var t in b.FootprintTiles()) World.SetOccupant(t, b.Id);
            OnLogisticPlaced(b);
            return b;
        }

        internal Player RestorePlayer(int id, SubPos pos)
        {
            var p = new Player { Pos = pos, Inventory = new Inventory(Db, Balance.PlayerInventorySlots) };
            RegisterWithId(p, id);
            Players.Add(p);
            return p;
        }
    }
}
