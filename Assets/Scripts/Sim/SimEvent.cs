using NewGen.Sim.Core;

namespace NewGen.Sim
{
    public enum SimEventType : byte
    {
        EntityAdded, EntityRemoved, EntityStateChanged, TileChanged, PlayerDied, Alert, ResearchFinished, ItemPickedUp, Shot, Hit
    }

    /// <summary>Things the view/UI should react to. The sim appends; the view drains once per frame.</summary>
    public struct SimEvent
    {
        public SimEventType Type;
        public int EntityId;
        public TilePos Tile;
        public string Text;      // alert key / tech id / item id
        public int Value;

        public static SimEvent Entity(SimEventType t, int id) => new SimEvent { Type = t, EntityId = id };
        public static SimEvent AtTile(SimEventType t, TilePos p) => new SimEvent { Type = t, Tile = p };
        public static SimEvent Alert(string key, TilePos p, int entity = 0) => new SimEvent { Type = SimEventType.Alert, Text = key, Tile = p, EntityId = entity };
    }
}
