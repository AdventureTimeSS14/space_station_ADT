using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.HTN;

// be_aggressive()
public sealed partial class BubblegumAggressivePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private MobStateSystem _mobState = default!;

    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public bool IsAggressive = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _mobState = sysManager.GetEntitySystem<MobStateSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var aggressive = false;

        if (_entManager.TryGetComponent<BubblegumComponent>(owner, out var boss)
            && boss.EnrageEndsAt > _timing.CurTime)
        {
            aggressive = true;
        }
        else if (blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            if (_entManager.Deleted(target))
                aggressive = false;
            else if (_mobState.IsIncapacitated(target))
                aggressive = true;
        }

        return aggressive == IsAggressive;
    }
}
