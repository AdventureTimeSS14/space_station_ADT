using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components; // ADT-Tweak
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

/// <summary>
/// Operator that wields or unwields a weapon.
/// </summary>
public sealed partial class WieldOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// If true, tries to wield the item. If false, tries to unwield it.
    /// </summary>
    [DataField]
    public bool Wield = true;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // ADT-Tweak-start
        var handsSystem = _entManager.System<HandsSystem>();
        EntityUid? weaponUid = null;

        if (blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager) &&
            handsSystem.TryGetHeldItem(owner, activeHand, out var held))
        {
            weaponUid = held;
        }
        else if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            if (handsComp.ActiveHandId != null && handsSystem.TryGetHeldItem(owner, handsComp.ActiveHandId, out held))
            {
                weaponUid = held;
            }
            else
            {
                foreach (var (handName, _) in handsComp.Hands)
                {
                    if (handsSystem.TryGetHeldItem(owner, handName, out var handHeld) &&
                        _entManager.HasComponent<WieldableComponent>(handHeld))
                    {
                        weaponUid = handHeld;
                        handsSystem.TrySetActiveHand((owner, handsComp), handName);
                        break;
                    }
                }
            }
        }

        if (weaponUid == null)
            return HTNOperatorStatus.Failed;
        // ADT-Tweak-end

        if (!_entManager.TryGetComponent<WieldableComponent>(weaponUid, out var wieldable))
            return HTNOperatorStatus.Failed;

        var wieldableSystem = _entManager.System<SharedWieldableSystem>();

        if (Wield)
        {
            if (wieldable.Wielded)
                return HTNOperatorStatus.Finished; // ADT-Tweak

            return wieldableSystem.TryWield((weaponUid.Value, wieldable), owner)
                ? HTNOperatorStatus.Finished
                : HTNOperatorStatus.Failed;
        }

        if (!wieldable.Wielded)
            return HTNOperatorStatus.Finished; // ADT-Tweak

        return wieldableSystem.TryUnwield((weaponUid.Value, wieldable), owner)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
