using System.Numerics;
using Content.Server.ADT.NPC.Helpers;
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
        var ignored = blackboard.TryGetValue<HashSet<EntityUid>>(ADTFindWeaponOperator.IgnoredKey, out var stored, _entManager)
            ? stored
            : new HashSet<EntityUid>();

        ignored.Add(target);
        blackboard.SetValue(ADTFindWeaponOperator.IgnoredKey, ignored);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(Key, out var target, _entManager) || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        var hands = _entManager.System<SharedHandsSystem>();
        var transform = _entManager.System<SharedTransformSystem>();

        var near = Vector2.Distance(
            transform.GetMapCoordinates(owner).Position,
            transform.GetMapCoordinates(target).Position) <= MaxDistance;

        if (!near)
        {
            Ignore(blackboard, target);
            return HTNOperatorStatus.Failed;
        }

        if (!ADTNpcItemHelper.MakeRoom(_entManager, owner, out var hand) || hand == null)
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
