using Content.Shared.EntityConditions;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects.Conditions;

public sealed partial class ADTDepartmentConditionSystem : EntityConditionSystem<MindContainerComponent, ADTDepartmentCondition>
{
    [Dependency] private SharedJobSystem _job = default!;

    protected override void Condition(Entity<MindContainerComponent> entity, ref EntityConditionEvent<ADTDepartmentCondition> args)
    {
        args.Result = entity.Comp.Mind is { } mind && _job.MindIsInDepartment(mind, args.Condition.Department);
    }
}

public sealed partial class ADTDepartmentCondition : EntityConditionBase<ADTDepartmentCondition>
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}
