using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Core;

namespace NewGen.Sim.Entities
{
    /// <summary>
    /// Burns an item for power (the coal burner). It produces its full output while something is
    /// burning and nothing at all when the fuel runs out, so a factory that forgets to feed it
    /// browns out.
    /// </summary>
    public sealed class FuelGeneratorBuilding : Building
    {
        public Inventory Fuel;
        /// <summary>Ticks of burn left in the item currently in the firebox.</summary>
        public int BurnLeft;
        /// <summary>How long the item currently burning lasts in total, for the fuel gauge.</summary>
        public int BurnTotal;

        public override Inventory InputInventory => Fuel;
        public bool Burning => BurnLeft > 0;
        public int BurnPermille => BurnTotal == 0 ? 0 : BurnLeft * 1000 / BurnTotal;

        public override bool Accepts(string item) => CanBurn(item) && Fuel.CanAdd(item, 1);
        public ContentDatabase Db;
        bool CanBurn(string item) { var def = Db?.Item(item); return def != null && def.IsFuel; }

        public override int PowerOutputNow() => !IsDestroyed && Burning ? Def.PowerOutput : 0;

        public override void Tick(Simulation sim)
        {
            if (BurnLeft > 0)
            {
                BurnLeft--;
                WorkedThisTick = true;
                return;
            }
            // light the next item
            string next = Fuel.TakeAny(CanBurn);
            if (next == null) return;
            BurnTotal = BurnLeft = sim.Db.Item(next).FuelTicks;
            WorkedThisTick = true;
        }

        public override void CollectContents(List<ItemStack> into)
        {
            if (Fuel != null) into.AddRange(Fuel.ToStacks());
        }
    }

    /// <summary>
    /// Stores surplus power and hands it back when the generators cannot keep up, so a factory
    /// with bursty demand does not stutter. Charge is in watt-ticks (30 watt-ticks = 1 joule).
    /// </summary>
    public sealed class BatteryBuilding : Building
    {
        public long Charge;
        public long Capacity => Def.EnergyCapacity;
        public int Throughput => Def.MaxThroughput;
        public int ChargePermille => Capacity == 0 ? 0 : (int)(Charge * 1000 / Capacity);

        /// <summary>Watts it could absorb this tick.</summary>
        public long RoomToCharge() => IsDestroyed ? 0 : System.Math.Min(Throughput, Capacity - Charge);
        /// <summary>Watts it could supply this tick.</summary>
        public long AvailableToDischarge() => IsDestroyed ? 0 : System.Math.Min(Throughput, Charge);

        public void AddCharge(long watts)
        {
            Charge += watts;
            if (Charge < 0) Charge = 0;
            if (Charge > Capacity) Charge = Capacity;
        }
    }
}
