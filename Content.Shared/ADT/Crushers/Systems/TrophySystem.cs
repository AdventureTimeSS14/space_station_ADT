using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Weapons.KineticCooldown;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TrophyHolderSystem _trophyHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TrophyHolderComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<TrophyHolderComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<TrophyHolderComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<KineticCooldownTrophyEffectComponent, TrophyAlteredEvent>(OnTrophyAltered);
    }

    private void OnTrophyAltered(Entity<KineticCooldownTrophyEffectComponent> ent, ref TrophyAlteredEvent args)
    {
        if (!TryComp<ADTKineticCooldownComponent>(args.Holder, out var cooldown))
            return;

        if (ent.Comp.MeleeCoefficient <= 0f || ent.Comp.RangedCoefficient <= 0f)
            return;

        switch (args.Alteration)
        {
            case TrophyAlteredType.Inserted:
                if (ent.Comp.Applied)
                    return;

                cooldown.MeleeMultiplierModifier /= ent.Comp.MeleeCoefficient;
                cooldown.RangedMultiplierModifier /= ent.Comp.RangedCoefficient;

                ent.Comp.Applied = true;
                break;

            case TrophyAlteredType.Removed:
                if (!ent.Comp.Applied)
                    return;

                cooldown.MeleeMultiplierModifier *= ent.Comp.MeleeCoefficient;
                cooldown.RangedMultiplierModifier *= ent.Comp.RangedCoefficient;

                ent.Comp.Applied = false;
                break;
        }

        Dirty(args.Holder, cooldown);
        Dirty(ent);
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

        DetonateMarks(ent, ref args);
    }

    private void DetonateMarks(Entity<TrophyHolderComponent> ent, ref MeleeHitEvent args)
    {
        if (!_net.IsServer)
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner || target == args.User)
                continue;

            if (!TryComp<DamageMarkerComponent>(target, out var marker) || marker.Marker != ent.Owner)
                continue;

            foreach (var trophy in _trophyHolder.GetCurrentTrophies(ent))
            {
                foreach (var effect in trophy.Comp.Effects)
                {
                    effect.OnMarkDetonation(trophy, ent, args.User, target, EntityManager, ref args);
                }
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
