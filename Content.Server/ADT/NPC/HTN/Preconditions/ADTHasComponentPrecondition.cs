using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTHasComponentPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public string? Key;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        EntityUid uid;

        if (Key == null)
            uid = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        else if (!blackboard.TryGetValue<EntityUid>(Key, out uid, _entManager))
            return false;

        foreach (var entry in Components.Values)
        {
            if (!_entManager.HasComponent(uid, entry.Component.GetType()))
                return Invert;
        }

        return !Invert;
    }
}
