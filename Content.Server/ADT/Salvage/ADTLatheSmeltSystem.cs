using Content.Server.Lathe;
using Content.Shared.ADT.Salvage;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage;

public sealed class ADTLatheSmeltSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public const int MaxSmeltAmount = 30;

    public override void Initialize()
    {
        Subs.BuiEvents<MiningPointsLatheComponent>(LatheUiKey.Key, subs =>
        {
            subs.Event<ADTLatheSmeltAllMessage>(OnSmeltAll);
        });
    }

    private void OnSmeltAll(Entity<MiningPointsLatheComponent> ent, ref ADTLatheSmeltAllMessage args)
    {
        if (!TryComp<LatheComponent>(ent, out var lathe))
            return;

        if (!_proto.TryIndex(args.Recipe, out var recipe))
            return;

        if (!_lathe.TryGetAvailableRecipes(ent, out var available, lathe) || !available.Contains(args.Recipe))
            return;

        if (GetMaxAmount((ent, lathe), recipe) is not { } amount)
            return;

        if (!_lathe.TryAddToQueue(ent, recipe, amount, lathe))
            return;

        while (lathe.Queue.Count > 0 || lathe.CurrentRecipe != null)
        {
            if (lathe.CurrentRecipe == null && !_lathe.TryStartProducing(ent, lathe))
                break;

            _lathe.FinishProducing(ent, lathe);
        }
    }

    private int? GetMaxAmount(Entity<LatheComponent> ent, LatheRecipePrototype recipe)
    {
        if (recipe.Materials.Count == 0)
            return null;

        var amount = MaxSmeltAmount;
        foreach (var (material, needed) in recipe.Materials)
        {
            var perItem = SharedLatheSystem.AdjustMaterial(needed, recipe.ApplyMaterialDiscount, ent.Comp.MaterialUseMultiplier);
            if (perItem <= 0)
                continue;

            amount = Math.Min(amount, _materialStorage.GetMaterialAmount(ent.Owner, material) / perItem);
        }

        return amount > 0 ? amount : null;
    }
}
