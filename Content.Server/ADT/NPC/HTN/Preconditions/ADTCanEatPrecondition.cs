using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Body.Components;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTCanEatPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_entManager.TryGetComponent(owner, out InternalsComponent? internals) && internals.GasTankEntity != null)
            return false;

        return _entManager.System<ADTNpcGearSystem>().FindFood(owner) != null;
    }
}
