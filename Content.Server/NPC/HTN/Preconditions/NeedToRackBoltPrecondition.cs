using Content.Server.Hands.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the NPC's gun needs bolt racking - either bolt is open OR bolt is closed but chamber is empty.
/// Returns true if gun needs racking to prepare for firing.
/// </summary>
public sealed partial class NeedToRackBoltPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

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
                        _entManager.HasComponent<ChamberMagazineAmmoProviderComponent>(handHeld))
                    {
                        heldEntity = handHeld;
                        break;
                    }
                }
            }
        }

        if (heldEntity == null)
        // ADT-Tweak-end
            return false;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(heldEntity, out var chamberMagazine))
            return false;

        if (!chamberMagazine.CanRack)
            return false;

        // ADT-Tweak-start
        if (chamberMagazine.BoltClosed == false)
            return true;
        // ADT-Tweak-end

        var gunSystem = _entManager.System<SharedGunSystem>();
        var chamberEntity = gunSystem.GetChamberEntity(heldEntity.Value);
        // ADT-Tweak-start
        var hasLiveRound = false;
        if (chamberEntity != null)
        {
            if (_entManager.TryGetComponent<CartridgeAmmoComponent>(chamberEntity, out var cartridge))
                hasLiveRound = !cartridge.Spent;
            else
                hasLiveRound = true;
        }

        if (hasLiveRound)
            return false;

        var itemSlots = _entManager.System<ItemSlotsSystem>();
        var magEnt = itemSlots.GetItemOrNull(heldEntity.Value, "gun_magazine");
        if (magEnt == null)
            return false;

        var ev = new GetAmmoCountEvent();
        _entManager.EventBus.RaiseLocalEvent(magEnt.Value, ref ev, false);
        return ev.Count > 0;
        // ADT-Tweak-end
    }
}
