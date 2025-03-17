using Content.Shared.Projectiles;
using Content.Shared.ADT.Crawling;
using Content.Shared.Throwing;
using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;
using Content.Shared.Alert;
using Content.Shared.Climbing.Components;
using Content.Shared.Popups;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Climbing.Events;

namespace Content.Shared.ADT.Collision.Knockdown;

public sealed class KnockdownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<KnockdownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, KnockdownOnCollideComponent component)
    {
        _standing.Down(target, dropHeldItems: false);
    }
}
