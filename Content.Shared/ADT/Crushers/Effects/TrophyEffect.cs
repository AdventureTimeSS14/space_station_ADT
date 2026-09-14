using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class TrophyEffect
{
    public abstract FormattedMessage GetDescription();

    public virtual void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
    }

    public virtual void OnMarkDetonation(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityUid user,
        EntityUid target,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
    }

    public virtual void OnMarkedMeleeDamage(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        Entity<DamageMarkerComponent> marker,
        EntityManager entManager,
        ref GetMeleeDamageEvent args)
    {
    }

    public virtual void OnMarkedGunShot(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        Entity<DamageMarkerComponent> marker,
        EntityManager entManager,
        ref GunShotEvent args)
    {
    }

    public virtual void OnGunShot(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref GunShotEvent args)
    {
    }

    public virtual void OnProjectileHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref ProjectileHitEvent args)
    {
    }

    public virtual void OnGunRefreshModifiers(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref GunRefreshModifiersEvent args)
    {
    }

    protected EntityUid GetOrigin(Entity<TrophyHolderComponent> holder, EntityManager entManager)
    {
        return entManager.TransformQuery.GetComponent(holder).ParentUid;
    }
}
