using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.EntityConditions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;
public sealed partial class HereticOrGhoulConditionSystem : EntityConditionSystem<MetaDataComponent, HereticOrGhoulCondition>
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<HereticOrGhoulCondition> args)
    {
        args.Result = _heretic.IsHereticOrGhoul(entity);
    }
}

/// <inheritdoc cref="HereticOrGhoulConditionSystem"/>
[UsedImplicitly]
public sealed partial class HereticOrGhoulCondition : EntityConditionBase<HereticOrGhoulCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-has-component",
            ("comp", Loc.GetString("reagent-comp-condition-heretic-or-ghoul")),
            ("invert", Inverted));
    }
}
