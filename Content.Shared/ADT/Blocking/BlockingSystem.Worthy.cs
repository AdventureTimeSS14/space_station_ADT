using Content.Shared.ADT.Armor;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    private bool CanShieldProtect(EntityUid shield, EntityUid user)
    {
        var ev = new ArmorProtectAttemptEvent(user);
        RaiseLocalEvent(shield, ref ev);
        return !ev.Cancelled;
    }
}
