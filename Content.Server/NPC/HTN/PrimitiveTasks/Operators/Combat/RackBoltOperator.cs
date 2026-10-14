using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components; // ADT-Tweak
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

/// <summary>
/// Operator that racks the bolt of a gun with <see cref="ChamberMagazineAmmoProviderComponent"/>.
/// </summary>
public sealed partial class RackBoltOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // ADT-Tweak-start
        var handsSystem = _entManager.System<HandsSystem>();
        EntityUid? gunUid = null;

        if (blackboard.TryGetValue<string>(NPCBlackboard.ActiveHand, out var activeHand, _entManager) &&
            handsSystem.TryGetHeldItem(owner, activeHand, out var held))
        {
            gunUid = held;
        }
        else if (_entManager.TryGetComponent<HandsComponent>(owner, out var handsComp))
        {
            if (handsComp.ActiveHandId != null && handsSystem.TryGetHeldItem(owner, handsComp.ActiveHandId, out held))
            {
                gunUid = held;
            }
            else
            {
                foreach (var (handName, _) in handsComp.Hands)
                {
                    if (handsSystem.TryGetHeldItem(owner, handName, out var handHeld) &&
                        _entManager.HasComponent<ChamberMagazineAmmoProviderComponent>(handHeld))
                    {
                        gunUid = handHeld;
                        handsSystem.TrySetActiveHand((owner, handsComp), handName);
                        break;
                    }
                }
            }
        }

        if (gunUid == null)
            return HTNOperatorStatus.Failed;
        // ADT-Tweak-end

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamberMagazine))
            return HTNOperatorStatus.Failed;

        var gunSystem = _entManager.System<SharedGunSystem>();
        gunSystem.UseChambered(gunUid.Value, chamberMagazine, owner);

        return HTNOperatorStatus.Finished;
    }
}
