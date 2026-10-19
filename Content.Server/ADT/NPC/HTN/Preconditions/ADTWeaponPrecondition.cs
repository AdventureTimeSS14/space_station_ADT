using Content.Server.ADT.NPC.Helpers;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTWeaponPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField(required: true)]
    public ADTNpcWeaponCheck Check;

    [DataField]
    public bool Invert;

    [DataField]
    public bool Strict;

    private static GameTick _cacheTick;
    private static readonly Dictionary<(EntityUid, ADTNpcWeaponRole, bool, ADTNpcWeaponCheck), bool> Cache = new();

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (_timing.CurTick != _cacheTick)
        {
            Cache.Clear();
            _cacheTick = _timing.CurTick;
        }

        var key = (owner, Role, Strict, Check);

        if (!Cache.TryGetValue(key, out var result))
        {
            result = Evaluate(owner);
            Cache[key] = result;
        }

        return result ^ Invert;
    }

    private bool Evaluate(EntityUid owner)
    {
        if (Check == ADTNpcWeaponCheck.ActiveNonWeapon)
        {
            return _entManager.System<SharedHandsSystem>().GetActiveItem(owner) is { } active &&
                   !_entManager.HasComponent<VirtualItemComponent>(active) &&
                   ADTNpcWeaponHelper.Classify(_entManager, active) == null;
        }

        if (ADTNpcWeaponHelper.Resolve(_entManager, owner, Role, Strict) is not { } found)
            return false;

        var gun = found.Item;
        var melee = Role == ADTNpcWeaponRole.Melee;

        switch (Check)
        {
            case ADTNpcWeaponCheck.Exists:
                return true;
            case ADTNpcWeaponCheck.Held:
                return found.Hand != null;
            case ADTNpcWeaponCheck.Active:
                return _entManager.System<SharedHandsSystem>().GetActiveItem(owner) == gun;
        }

        if (melee)
            return Check is ADTNpcWeaponCheck.HasAmmo or ADTNpcWeaponCheck.Usable;

        ADTNpcWeaponHelper.GetAmmo(_entManager, gun, out var count, out var capacity);

        switch (Check)
        {
            case ADTNpcWeaponCheck.HasAmmo:
                return count > 0;
            case ADTNpcWeaponCheck.Empty:
                return found.Hand != null && count <= 0;
            case ADTNpcWeaponCheck.NotFull:
                return capacity > 0 && count < capacity;
            case ADTNpcWeaponCheck.CanReload:
                return capacity > 0 && count < capacity && ADTNpcWeaponHelper.HasReserve(_entManager, owner, gun);
            case ADTNpcWeaponCheck.Usable:
                return count > 0 || (capacity > 0 && ADTNpcWeaponHelper.HasReserve(_entManager, owner, gun));
            case ADTNpcWeaponCheck.Magazine:
                return ADTNpcWeaponHelper.IsMagazineGun(_entManager, gun, out _);
            case ADTNpcWeaponCheck.Loose:
                return ADTNpcWeaponHelper.IsLooseGun(_entManager, gun);
            default:
                return false;
        }
    }
}
