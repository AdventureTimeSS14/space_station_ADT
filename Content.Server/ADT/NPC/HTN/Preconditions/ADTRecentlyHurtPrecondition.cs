using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTRecentlyHurtPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public float Seconds = 2f;

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var hurt = _entManager.TryGetComponent(owner, out ADTNpcCombatMemoryComponent? memory) &&
                   memory.Hurt &&
                   (_timing.CurTime - memory.LastHurt).TotalSeconds < Seconds;

        return hurt ^ Invert;
    }
}
