using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.ADT.Grab;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ADTGrabbedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public bool IsGrabbed = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var grabbed = _entManager.TryGetComponent<GrabbableComponent>(owner, out var grabbable)
            && grabbable.GrabStage != GrabStage.No;

        return grabbed == IsGrabbed;
    }
}
