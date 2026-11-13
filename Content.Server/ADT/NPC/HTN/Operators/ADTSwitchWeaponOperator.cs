using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTSwitchWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return _entManager.System<ADTNpcGearSystem>().SwitchTo(owner, Role)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
