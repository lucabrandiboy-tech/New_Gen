using System.Collections.Generic;
using NewGen.Sim.Content;
using NewGen.Sim.Entities;
using NewGen.Sim.Systems;

namespace NewGen.Sim
{
    public sealed partial class Simulation
    {
        public ResearchState Research;
        public readonly HashSet<string> UnlockedRecipes = new HashSet<string>();

        partial void InitSystems()
        {
            Research = new ResearchState(Db, Balance.ResearchQueueMax);
        }

        private partial Building CreateMachine(BuildingDef def)
        {
            switch (def.Kind)
            {
                case BuildingKind.Generator: return new GeneratorBuilding();
                case BuildingKind.Drill: return new DrillBuilding();
                case BuildingKind.Crafter:
                    {
                        var c = new CrafterBuilding { Inputs = new Inventory(Db, def.InputSlots), Outputs = new Inventory(Db, def.OutputSlots) };
                        for (int i = 0; i < c.Inputs.SlotCount; i++) c.Inputs.Filters[i] = "__none__";   // nothing accepted until a recipe is chosen
                        return c;
                    }
                case BuildingKind.DataCenter:
                    {
                        var dc = new DataCenterBuilding { Cells = new Inventory(Db, def.InputSlots), CellsDb = Db };
                        return dc;
                    }
                default: return null;
            }
        }

        partial void TickResearch() => Research.CheckCompletion(this);

        /// <summary>Recipes a crafter type can currently use.</summary>
        public List<RecipeDef> AvailableRecipes(string crafterType)
        {
            var list = new List<RecipeDef>();
            foreach (var r in Db.Recipes.Values)
                if (r.Crafter == crafterType && Research.IsRecipeUnlocked(r, this)) list.Add(r);
            return list;
        }
    }
}
