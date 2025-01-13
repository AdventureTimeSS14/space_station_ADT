using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Containers;
using Content.Shared.ADT.Crawling; // ADT Anti-Lying-Warrior
using Content.Shared.Mobs.Systems; // ADT Anti-Lying-Warrior

namespace Content.Shared.Damage.Components;

public sealed class RequireProjectileTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!; // ADT Anti-Lying-Warrior

    public override void Initialize()
    {
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(Entity<RequireProjectileTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Active)
            return;

        var other = args.OtherEntity;
        if (TryComp(other, out ProjectileComponent? projectile))
        {
            // ADT Crawling abuse fix start
            if (TryComp<TargetedProjectileComponent>(other, out var targeted) && targeted.TargetCoords != null)
            {
                foreach (var item in _lookup.GetEntitiesInRange(targeted.TargetCoords.Value, 0.5f))
                {
                    if (item == ent.Owner)
                        return;
                }
                // ADT Crawling abuse fix end
                // ADT ALW Tweak
                var weapon = projectile.Weapon;
                var alwTarget = targeted.Target;
                if (weapon.HasValue && HasComp<AntiLyingWarriorComponent>(weapon) && _mobState.IsDead(alwTarget))
                    return;
            }
            // ADT ALW Tweak
            // Prevents shooting out of while inside of crates
            var shooter = projectile.Shooter;
            if (!shooter.HasValue)
                return;

            // ProjectileGrenades delete the entity that's shooting the projectile,
            // so it's impossible to check if the entity is in a container
            if (TerminatingOrDeleted(shooter.Value))
                return;

            if (!_container.IsEntityOrParentInContainer(shooter.Value))
                args.Cancelled = true;
        }
    }

    private void SetActive(Entity<RequireProjectileTargetComponent> ent, bool value)
    {
        if (ent.Comp.Active == value)
            return;

        ent.Comp.Active = value;
        Dirty(ent);
    }

    private void StandingBulletHit(Entity<RequireProjectileTargetComponent> ent, ref StoodEvent args)
    {
        SetActive(ent, false);
    }

    private void LayingBulletPass(Entity<RequireProjectileTargetComponent> ent, ref DownedEvent args)
    {
        SetActive(ent, true);
    }
}
