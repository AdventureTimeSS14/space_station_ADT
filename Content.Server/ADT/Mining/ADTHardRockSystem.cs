using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Mining;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Server.ADT.Mining;

public sealed class ADTHardRockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTHardRockComponent, StartCollideEvent>(OnHardRockCollide);
    }

    private void OnHardRockCollide(Entity<ADTHardRockComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard || args.OtherFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        if (!TryComp<GatheringProjectileComponent>(args.OtherEntity, out var projectile))
            return;

        projectile.Amount = 0;
        _popup.PopupEntity(Loc.GetString("adt-hard-rock-popup-resistant"), ent.Owner);
    }

    public bool IsHardRock(EntityUid uid)
    {
        return HasComp<ADTHardRockComponent>(uid);
    }
}
