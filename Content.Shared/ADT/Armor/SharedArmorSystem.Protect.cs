using Content.Shared.ADT.Armor;

namespace Content.Shared.Armor;

public abstract partial class SharedArmorSystem
{
    private bool CanProtect(EntityUid armor, EntityUid wearer)
    {
        var ev = new ArmorProtectAttemptEvent(wearer);
        RaiseLocalEvent(armor, ref ev);
        return !ev.Cancelled;
    }
}
