using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Server.ADT.Mining;

public sealed class ADTHardRockSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTHardRockComponent, StartCollideEvent>(OnHardRockCollide);
    }

    private void OnHardRockCollide(Entity<ADTHardRockComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OurFixture.Hard || args.OtherFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        if (!TryComp<GatheringProjectileComponent>(args.OtherEntity, out var projectile))
            return;

        if (TryComp<ADTHardRockPiercingComponent>(args.OtherEntity, out var piercing))
        {
            _damageable.TryChangeDamage(ent.Owner, piercing.Damage, true);
            return;
        }

        projectile.Amount = 0;
        _popup.PopupEntity(Loc.GetString("adt-hard-rock-popup-resistant"), ent.Owner);
    }

    public bool IsHardRock(EntityUid uid)
    {
        return HasComp<ADTHardRockComponent>(uid);
    }
}
