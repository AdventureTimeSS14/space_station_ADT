using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTWeaponPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    private ADTNpcGearSystem _gear = default!;
    private SharedHandsSystem _hands = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField(required: true)]
    public ADTNpcWeaponCheck Check;

    [DataField]
    public bool Invert;

    [DataField]
    public bool Strict;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
        _hands = sysManager.GetEntitySystem<SharedHandsSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return Evaluate(owner) ^ Invert;
    }

    private bool Evaluate(EntityUid owner)
    {
        if (Check == ADTNpcWeaponCheck.ActiveNonWeapon)
        {
            return _hands.GetActiveItem(owner) is { } active &&
                   !_entManager.HasComponent<VirtualItemComponent>(active) &&
                   _gear.Classify(active) == null;
        }

        if (_gear.Resolve(owner, Role, Strict) is not { } found)
            return false;

        var gun = found.Item;

        switch (Check)
        {
            case ADTNpcWeaponCheck.Exists:
                return true;
            case ADTNpcWeaponCheck.Held:
                return found.Hand != null;
            case ADTNpcWeaponCheck.Active:
                return _hands.GetActiveItem(owner) == gun;
        }

        if (Role == ADTNpcWeaponRole.Melee)
            return Check is ADTNpcWeaponCheck.HasAmmo or ADTNpcWeaponCheck.Usable;

        _gear.GetAmmo(gun, out var count, out var capacity);

        switch (Check)
        {
            case ADTNpcWeaponCheck.HasAmmo:
                return count > 0;
            case ADTNpcWeaponCheck.Empty:
                return found.Hand != null && count <= 0;
            case ADTNpcWeaponCheck.NotFull:
                return capacity > 0 && count < capacity;
            case ADTNpcWeaponCheck.CanReload:
                return capacity > 0 && count < capacity && _gear.HasReserve(owner, gun);
            case ADTNpcWeaponCheck.Usable:
                return count > 0 || (capacity > 0 && _gear.HasReserve(owner, gun));
            case ADTNpcWeaponCheck.Magazine:
                return _gear.IsMagazineGun(gun, out _);
            case ADTNpcWeaponCheck.Loose:
                return _gear.IsLooseGun(gun);
            default:
                return false;
        }
    }
}
