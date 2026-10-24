// ADT-Tweak-start
using System.Threading;
using System.Threading.Tasks;
// ADT-Tweak-end
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components; // ADT-Tweak

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;

/// <summary>
/// Drops the active hand entity underneath us.
/// </summary>
public sealed partial class DropOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    // ADT-Tweak-start
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner, _entManager);
        var handsSystem = _entManager.System<HandsSystem>();

        string? handId = null;
        if (blackboard.TryGetValue(NPCBlackboard.ActiveHand, out string? activeHand, _entManager))
            handId = activeHand;
        else if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
            handId = handsComp.ActiveHandId;

        if (handId != null && handsSystem.TryGetHeldItem(owner, handId, out var held))
        {
            if (_entManager.HasComponent<UnremoveableComponent>(held))
                return (false, null);
        }

        return (true, null);
    }
    // ADT-Tweak-end

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValueOrDefault<EntityUid>(NPCBlackboard.Owner, _entManager);
        // TODO: Need some sort of interaction cooldown probably.
        var handsSystem = _entManager.System<HandsSystem>();

        // ADT-Tweak-start
        string? activeHand = null;
        if (blackboard.TryGetValue(NPCBlackboard.ActiveHand, out string? hand, _entManager))
            activeHand = hand;
        else if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
            activeHand = handsComp.ActiveHandId;

        if (activeHand == null)
            return HTNOperatorStatus.Finished;

        if (handsSystem.TryGetHeldItem(owner, activeHand, out var held) &&
            _entManager.HasComponent<UnremoveableComponent>(held))
        {
            return HTNOperatorStatus.Failed;
        }
        // ADT-Tweak-end

        if (handsSystem.TryDrop(owner))
        {
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Failed;
    }
}
