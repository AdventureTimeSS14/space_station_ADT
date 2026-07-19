using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophySystem : EntitySystem
{
    [Dependency] private readonly TrophyHolderSystem _trophyHolder = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TrophyHolderComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<TrophyHolderComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<TrophyHolderComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<UseDelayTrophyEffectComponent, TrophyAlteredEvent>(OnTrophyAltered);
    }

    private void OnTrophyAltered(Entity<UseDelayTrophyEffectComponent> ent, ref TrophyAlteredEvent args)
    {
        if (!TryComp<UseDelayComponent>(args.Holder, out var useDelay))
            return;

        switch (args.Alteration)
        {
            case TrophyAlteredType.Inserted:
                if (!_useDelay.TryGetDelayInfo((args.Holder, useDelay), out var delayInfo))
                    return;

                if (ent.Comp.Coefficient <= 0f)
                    return;

                ent.Comp.OriginalDelay = delayInfo.Length;
                Dirty(ent);

                var newLength = delayInfo.Length / ent.Comp.Coefficient;
                _useDelay.SetLength((args.Holder, useDelay), newLength);
                break;

            case TrophyAlteredType.Removed:
                if (ent.Comp.OriginalDelay == null)
                    return;

                _useDelay.SetLength((args.Holder, useDelay), ent.Comp.OriginalDelay.Value);

                ent.Comp.OriginalDelay = null;
                Dirty(ent);
                break;
        }
    }

    private void OnMeleeHit(Entity<TrophyHolderComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var trophy in _trophyHolder.GetCurrentTrophies(ent))
        {
            foreach (var effect in trophy.Comp.Effects)
            {
                effect.OnMeleeHit(trophy, ent, EntityManager, ref args);
            }
        }
    }

    private void OnProjectileHit(Entity<DamageMarkerOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter == null || args.Shooter == args.Target)
            return;

        if (!TryComp<ProjectileComponent>(ent, out var projectile) || projectile.Weapon is null)
            return;

        if (!TryComp<TrophyHolderComponent>(projectile.Weapon.Value, out var trophyHolderComp))
            return;

        var trophyHolder = (projectile.Weapon.Value, trophyHolderComp);
        foreach (var trophy in _trophyHolder.GetCurrentTrophies(trophyHolder))
        {
            foreach (var effect in trophy.Comp.Effects)
            {
                effect.OnProjectileHit(trophy, trophyHolder, EntityManager, ref args);
            }
        }
    }

    private void OnGunRefreshModifiers(Entity<TrophyHolderComponent> ent, ref GunRefreshModifiersEvent args)
    {
        foreach (var trophy in _trophyHolder.GetCurrentTrophies(ent))
        {
            foreach (var effect in trophy.Comp.Effects)
            {
                effect.OnGunRefreshModifiers(trophy, ent, EntityManager, ref args);
            }
        }
    }

    private void OnGunShot(Entity<TrophyHolderComponent> ent, ref GunShotEvent args)
    {
        foreach (var trophy in _trophyHolder.GetCurrentTrophies(ent))
        {
            foreach (var effect in trophy.Comp.Effects)
            {
                effect.OnGunShot(trophy, ent, EntityManager, ref args);
            }
        }
    }
}
