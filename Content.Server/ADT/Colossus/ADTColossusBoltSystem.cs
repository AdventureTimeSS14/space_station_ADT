using Content.Server.ADT.Salvage.Systems;
using Content.Server.Destructible;
using Content.Shared.ADT.Colossus;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gatherable;
using Content.Shared.Gatherable.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.ADT.Colossus;

public sealed class ADTColossusBoltSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private GatherableSystem _gatherable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTColossusBoltComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ADTColossusBoltComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<ADTColossusBoltComponent> ent, ref PreventCollideEvent args)
    {
        if (TryComp<ProjectileComponent>(ent, out var projectile) && args.OtherEntity == projectile.Shooter)
            args.Cancelled = true;
    }

    private void OnProjectileHit(Entity<ADTColossusBoltComponent> ent, ref ProjectileHitEvent args)
    {
        var target = args.Target;

        if (TerminatingOrDeleted(target) || HasComp<MobStateComponent>(target))
            return;

        if (HasComp<GatherableComponent>(target))
        {
            if (ent.Comp.GatherRock)
                _gatherable.Gather(target);

            return;
        }

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        if (ent.Comp.DestroyWalls && _megafauna.IsWall(target))
        {
            var destroyedAt = _destructible.DestroyedAt(target);

            if (destroyedAt == FixedPoint2.MaxValue)
                return;

            var needed = destroyedAt - _damageable.GetTotalDamage((target, damageable));

            if (needed <= FixedPoint2.Zero)
                return;

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Structural", needed);
            _damageable.TryChangeDamage((target, damageable), damage, true, origin: args.Shooter);
            return;
        }

        var multiplier = _random.NextFloat(1f, ent.Comp.ObjectDamageMaxMultiplier);
        _damageable.TryChangeDamage((target, damageable), ent.Comp.ObjectDamage * multiplier, origin: args.Shooter);
    }
}
