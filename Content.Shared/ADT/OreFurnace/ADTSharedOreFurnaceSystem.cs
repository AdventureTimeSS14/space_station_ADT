using Content.Shared.ADT.OreFurnace.Components;
using Content.Shared.ADT.OreFurnace.Prototypes;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.OreFurnace;

public sealed class ADTSharedOreFurnaceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public List<OreSmeltRecipePrototype> GetRecipes(ADTOreFurnaceComponent component)
    {
        var recipes = new List<OreSmeltRecipePrototype>();

        foreach (var packId in component.Packs)
        {
            if (!_proto.TryIndex(packId, out var pack))
                continue;

            foreach (var recipeId in pack.Recipes)
            {
                if (!_proto.TryIndex(recipeId, out var recipe))
                    continue;

                if (!recipes.Contains(recipe))
                    recipes.Add(recipe);
            }
        }

        return recipes;
    }

    public bool HasRecipe(ADTOreFurnaceComponent component, ProtoId<OreSmeltRecipePrototype> recipe)
    {
        foreach (var packId in component.Packs)
        {
            if (_proto.TryIndex(packId, out var pack) && pack.Recipes.Contains(recipe))
                return true;
        }

        return false;
    }

    public string GetRecipeName(OreSmeltRecipePrototype recipe)
    {
        if (recipe.Name is { } name)
            return Loc.GetString(name);

        return _proto.Index(recipe.Result).Name;
    }

    public int GetMaterialCost(ADTOreFurnaceComponent component, int needed)
    {
        return Math.Max(1, (int) MathF.Ceiling(needed * component.MaterialUseMultiplier));
    }

    public int GetMaxSmeltAmount(Entity<ADTOreFurnaceComponent> ent, OreSmeltRecipePrototype recipe)
    {
        if (recipe.Materials.Count == 0)
            return 0;

        var amount = ent.Comp.MaxSmeltAmount;

        foreach (var (material, needed) in recipe.Materials)
        {
            var cost = GetMaterialCost(ent.Comp, needed);
            amount = Math.Min(amount, _materialStorage.GetMaterialAmount(ent.Owner, material) / cost);

            if (amount <= 0)
                return 0;
        }

        return amount;
    }

    public int GetOutputCount(ADTOreFurnaceComponent component, int amount)
    {
        if (amount <= 0)
            return 0;

        return Math.Max(1, (int) MathF.Round(amount * component.OutputMultiplier));
    }

    public uint GetPointsGain(ADTOreFurnaceComponent component, OreSmeltRecipePrototype recipe, int amount)
    {
        if (amount <= 0 || recipe.MiningPoints == 0)
            return 0;

        return (uint) MathF.Round(recipe.MiningPoints * amount * component.PointsMultiplier);
    }
}
