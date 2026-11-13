using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTPickupWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public string Key = "Target";

    [DataField]
    public float MaxDistance = 2.5f;

    private const string TimerKey = "ADTPickupTimer";

    [DataField]
    public float RoomTimeout = 3f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
    }

    private void Ignore(NPCBlackboard blackboard, EntityUid target)
    {
        _entManager.System<ADTNpcGearSystem>().Ignore(blackboard, ADTNpcGearSystem.IgnoredWeaponsKey, target);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        var hands = _entManager.System<SharedHandsSystem>();
        var gear = _entManager.System<ADTNpcGearSystem>();

        if (!_entManager.System<SharedTransformSystem>().InRange(owner, target, MaxDistance))
        {
            Ignore(blackboard, target);
            return HTNOperatorStatus.Failed;
        }

        if (!gear.MakeRoom(owner, out var hand) || hand == null)
        {
            var timer = (blackboard.TryGetValue<float>(TimerKey, out var t, _entManager) ? t : 0f) + frameTime;
            blackboard.SetValue(TimerKey, timer);

            if (timer < RoomTimeout)
                return HTNOperatorStatus.Continuing;

            Ignore(blackboard, target);
            return HTNOperatorStatus.Failed;
        }

        if (hands.TryPickup(owner, target, hand, checkActionBlocker: false, animate: false))
        {
            hands.TrySetActiveHand(owner, hand);
            return HTNOperatorStatus.Finished;
        }

        Ignore(blackboard, target);
        return HTNOperatorStatus.Failed;
    }
}
