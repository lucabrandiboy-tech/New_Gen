namespace NewGen.Sim.World
{
    /// <summary>One map tile: 2 bytes. Terrain index into ContentDatabase.Terrains, resource index into Resources (0 = none).</summary>
    public struct Tile
    {
        public byte Terrain;
        public byte Resource;
        public Tile(byte terrain, byte resource) { Terrain = terrain; Resource = resource; }
        public bool Equals(Tile o) => Terrain == o.Terrain && Resource == o.Resource;
        public override bool Equals(object obj) => obj is Tile t && Equals(t);
        public override int GetHashCode() => Terrain << 8 | Resource;
    }
}
