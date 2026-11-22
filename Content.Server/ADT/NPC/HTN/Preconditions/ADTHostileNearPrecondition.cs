using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTHostileNearPrecondition : HTNPrecondition
{
    private ADTNpcCombatMemorySystem _memory = default!;

    [DataField]
    public float Range = 10f;

    [DataField]
    public bool Invert;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _memory = sysManager.GetEntitySystem<ADTNpcCombatMemorySystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _memory.HasHostileNear(owner, Range) ^ Invert;
    }
}
