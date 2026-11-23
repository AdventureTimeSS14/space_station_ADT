using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.ADT.Cooking.Systems;

public sealed partial class ADTWellFedSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTWellFedStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<ADTWellFedStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<StatusEffectContainerComponent, ADTSatiationRateModifyEvent>(OnRateModify);
    }

    private void OnApplied(Entity<ADTWellFedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshRates(args.Target, ent.Comp);
    }

    private void OnRemoved(Entity<ADTWellFedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshRates(args.Target, ent.Comp);
    }

    private void OnRateModify(Entity<StatusEffectContainerComponent> ent, ref ADTSatiationRateModifyEvent args)
    {
        float? best = null;
        foreach (var effect in _statusEffects.EnumerateStatusEffects<ADTWellFedStatusEffectComponent>(ent.AsNullable()))
        {
            if (!effect.Comp2.RateMultipliers.TryGetValue(args.Type, out var multiplier))
                continue;

            if (best == null || multiplier < best)
                best = multiplier;
        }

        if (best != null)
            args.Multiplier *= best.Value;
    }

    private void RefreshRates(EntityUid target, ADTWellFedStatusEffectComponent effect)
    {
        if (!TryComp<SatiationComponent>(target, out var satiation))
            return;

        foreach (var type in effect.RateMultipliers.Keys)
        {
            if (_satiation.GetValueOrNull((target, satiation), type) is { } value)
                _satiation.SetValue((target, satiation), type, value);
        }
    }
}
