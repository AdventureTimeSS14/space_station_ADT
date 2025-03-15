using Content.Shared.Construction.Components;
<<<<<<< HEAD
using Content.Shared.Damage;
using Content.Shared.Standing;
=======
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Timing;
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// This handles <see cref="MeleeThrowOnHitComponent"/>
/// </summary>
public sealed class MeleeThrowOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
<<<<<<< HEAD
    [Dependency] private readonly StandingStateSystem _standing = default!; // ADT-Changeling-Tweak
    [Dependency] private readonly DamageableSystem _damage = default!; // ADT-Changeling-Tweak

=======
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MeleeThrowOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<MeleeThrowOnHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(Entity<MeleeThrowOnHitComponent> weapon, ref MeleeHitEvent args)
    {
        // TODO: MeleeHitEvent is weird. Why is this even raised if we don't hit something?
        if (!args.IsHit)
            return;

        if (_delay.IsDelayed(weapon.Owner))
            return;

        if (args.HitEntities.Count == 0)
            return;

        var userPos = _transform.GetWorldPosition(args.User);
        foreach (var target in args.HitEntities)
        {
<<<<<<< HEAD
            var hitPos = _transform.GetMapCoordinates(hit).Position;
            var angle = args.Direction ?? hitPos - mapPos;
            if (angle == Vector2.Zero)
                continue;

            if (!CanThrowOnHit(ent, hit))
                continue;

            if (comp.UnanchorOnHit && HasComp<AnchorableComponent>(hit))
            {
                _transform.Unanchor(hit, Transform(hit));
            }

            RemComp<MeleeThrownComponent>(hit);
            var ev = new MeleeThrowOnHitStartEvent(args.User, ent);
            RaiseLocalEvent(hit, ref ev);
            var thrownComp = new MeleeThrownComponent
            {
                Velocity = angle.Normalized() * comp.Speed,
                Lifetime = comp.Lifetime,
                MinLifetime = comp.MinLifetime,
                CollideDamage = comp.CollideDamage,  // ADT tweak
                ToCollideDamage = comp.ToCollideDamage  // ADT tweak
            };
            AddComp(hit, thrownComp);

            if (comp.DownOnHit) // ADT tweak
                _standing.Down(hit);
=======
            var targetPos = _transform.GetMapCoordinates(target).Position;
            var direction = args.Direction ?? targetPos - userPos;
            ThrowOnHitHelper(weapon, args.User, target, direction);
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3
        }
    }

    private void OnThrowHit(Entity<MeleeThrowOnHitComponent> weapon, ref ThrowDoHitEvent args)
    {
        if (!weapon.Comp.ActivateOnThrown)
            return;

        if (!TryComp<PhysicsComponent>(args.Thrown, out var weaponPhysics))
            return;

        ThrowOnHitHelper(weapon, args.Component.Thrower, args.Target, weaponPhysics.LinearVelocity);
    }

    private void ThrowOnHitHelper(Entity<MeleeThrowOnHitComponent> ent, EntityUid? user, EntityUid target, Vector2 direction)
    {
        var attemptEvent = new AttemptMeleeThrowOnHitEvent(target, user);
        RaiseLocalEvent(ent.Owner, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

<<<<<<< HEAD
        if (ent.Comp.CollideDamage != null)   // ADT tweak
            _damage.TryChangeDamage(ent.Owner, ent.Comp.CollideDamage);
        if (ent.Comp.ToCollideDamage != null) // ADT tweak
            _damage.TryChangeDamage(args.OtherEntity, ent.Comp.ToCollideDamage);

        RemCompDeferred(ent, ent.Comp);
    }
=======
        var startEvent = new MeleeThrowOnHitStartEvent(ent.Owner, user);
        RaiseLocalEvent(target, ref startEvent);
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3

        if (ent.Comp.StunTime != null)
            _stun.TryParalyze(target, ent.Comp.StunTime.Value, false);

        if (direction == Vector2.Zero)
            return;

        _throwing.TryThrow(target, direction.Normalized() * ent.Comp.Distance, ent.Comp.Speed, user, unanchor: ent.Comp.UnanchorOnHit);
    }
}
