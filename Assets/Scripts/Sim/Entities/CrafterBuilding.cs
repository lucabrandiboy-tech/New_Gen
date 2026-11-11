using System.Collections.Generic;
using NewGen.Sim.Content;

namespace NewGen.Sim.Entities
{
    /// <summary>Smelter / assembler / encoder: runs one chosen recipe. Arms feed inputs and take outputs.</summary>
    public sealed class CrafterBuilding : Building
    {
        public Inventory Inputs, Outputs;
        public string RecipeId;
        public int Progress;          // permille-ticks
        public bool MissingInput;     // for the alert icon

        public override Inventory InputInventory => Inputs;
        public override Inventory OutputInventory => Outputs;

        public RecipeDef Recipe(ContentDatabase db) => RecipeId == null ? null : db.Recipe(RecipeId);

        public bool CanUseRecipe(RecipeDef r) => r != null && r.Crafter == Def.CrafterType;

        public void SetRecipe(Simulation sim, string recipeId, Inventory returnTo)
        {
            var r = sim.Db.Recipe(recipeId);
            if (recipeId != null && !CanUseRecipe(r)) return;
            // give back whatever was inside
            if (returnTo != null) { Inputs.TransferAllTo(returnTo); Outputs.TransferAllTo(returnTo); }
            RecipeId = recipeId; Progress = 0;
            for (int i = 0; i < Inputs.SlotCount; i++) Inputs.Filters[i] = r != null && i < r.Inputs.Count ? r.Inputs[i].Item : (r == null ? null : "__none__");
        }

        public override bool Accepts(string item) => RecipeId != null && IsRecipeInput(item) && Inputs.CanAdd(item, 1);

        /// <summary>Does the current recipe use this item as an input? (Filters are set by SetRecipe.)</summary>
        bool IsRecipeInput(string item)
        {
            foreach (var f in Inputs.Filters) if (f == item) return true;
            return false;
        }

        public override void Tick(Simulation sim)
        {
            var r = Recipe(sim.Db);
            MissingInput = false;
            if (r == null || PowerSatisfaction <= 0) return;
            if (Progress == 0)
            {
                if (!Inputs.HasAll(r.Inputs)) { MissingInput = true; return; }
                foreach (var o in r.Outputs) if (!Outputs.CanAdd(o.Item, o.Count)) return;   // output full
                Inputs.RemoveAll(r.Inputs);
            }
            Progress += PowerSatisfaction;
            WorkedThisTick = true;
            int needed = r.Ticks * 1000;
            if (Progress < needed) return;
            foreach (var o in r.Outputs) Outputs.Add(o.Item, o.Count);
            Progress = 0;
        }

        /// <summary>0..1000 progress of the current craft.</summary>
        public int ProgressPermille(ContentDatabase db)
        {
            var r = Recipe(db);
            return r == null || r.Ticks == 0 ? 0 : (int)((long)Progress / r.Ticks);
        }
    }
}
