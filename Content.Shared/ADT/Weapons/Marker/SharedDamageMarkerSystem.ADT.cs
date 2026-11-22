using Content.Shared.Damage;

namespace Content.Shared.Weapons.Marker;

public abstract partial class SharedDamageMarkerSystem
{
    public void ApplyMarker(EntityUid target, EntityUid weapon, DamageSpecifier damage, TimeSpan duration)
    {
        var marker = EnsureComp<DamageMarkerComponent>(target);
        marker.Damage = new DamageSpecifier(damage);
        marker.Marker = weapon;
        marker.EndTime = _timing.CurTime + duration;
        Dirty(target, marker);
    }
}
