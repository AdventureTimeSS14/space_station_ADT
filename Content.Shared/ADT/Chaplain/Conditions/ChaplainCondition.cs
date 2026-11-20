using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain.Conditions;

public sealed partial class ChaplainConditionSystem : EntityConditionSystem<MetaDataComponent, ChaplainCondition>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<ChaplainCondition> args)
    {
        args.Result = HasComp<ChaplainComponent>(entity);
    }
}

public sealed partial class ChaplainCondition : EntityConditionBase<ChaplainCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-has-component",
            ("comp", Loc.GetString("reagent-comp-condition-chaplain")),
            ("invert", Inverted));
    }
}
