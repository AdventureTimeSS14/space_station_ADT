using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components; // ADT-Tweak
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the item in the active hand has <see cref="WieldableComponent"/> and whether its
/// <see cref="WieldableComponent.Wielded"/> state matches the expected value.
/// When <see cref="Wielded"/> is false, also checks that the item can actually be wielded.
/// </summary>
public sealed partial class WieldedPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    /// <summary>
    /// The wield state to check for.
    /// </summary>
    [DataField]
    public bool Wielded = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // ADT-Tweak-start
        var handsSystem = _entManager.System<HandsSystem>();
        EntityUid? heldEntity = null;

        if (blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager) &&
            handsSystem.TryGetHeldItem(owner, activeHand, out var held))
        {
            heldEntity = held;
        }
        else if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            if (handsComp.ActiveHandId != null && handsSystem.TryGetHeldItem(owner, handsComp.ActiveHandId, out held))
            {
                heldEntity = held;
            }
            else
            {
                foreach (var (handName, _) in handsComp.Hands)
                {
                    if (handsSystem.TryGetHeldItem(owner, handName, out var handHeld) &&
                        _entManager.HasComponent<WieldableComponent>(handHeld))
                    {
                        heldEntity = handHeld;
                        break;
                    }
                }
            }
        }

        if (heldEntity == null)
            return false;
        // ADT-Tweak-end

        if (!_entManager.TryGetComponent<WieldableComponent>(heldEntity, out var wieldable))
            return false;

        if (Wielded)
            return wieldable.Wielded;

        if (wieldable.Wielded)
            return false;

        var wieldableSystem = _entManager.System<SharedWieldableSystem>();
        return wieldableSystem.CanWield((heldEntity.Value, wieldable), owner, quiet: true);
    }
}
