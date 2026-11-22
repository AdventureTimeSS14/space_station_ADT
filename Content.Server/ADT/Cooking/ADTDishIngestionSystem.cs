using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTDishIngestionSystem : EntitySystem
{
    [Dependency] private ADTDishSystem _dish = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDishComponent, IngestedEvent>(OnIngested);
    }

    private void OnIngested(Entity<ADTDishComponent> ent, ref IngestedEvent args)
    {
        if (_dish.GetQuality(ent.AsNullable()) is not { } quality)
            return;

        if (quality.WellFedEffect is not { } effect || quality.WellFedDuration <= TimeSpan.Zero)
            return;

        if (!TryComp<EdibleComponent>(ent, out var edible)
            || !_solution.TryGetSolution(ent.Owner, edible.Solution, out _, out var solution)
            || solution.Volume > FixedPoint2.Zero)
        {
            return;
        }

        if (_statusEffects.HasEffectComp<ADTWellFedStatusEffectComponent>(args.Target))
            return;

        _statusEffects.TryAddStatusEffectDuration(args.Target, effect, quality.WellFedDuration);
    }
}
