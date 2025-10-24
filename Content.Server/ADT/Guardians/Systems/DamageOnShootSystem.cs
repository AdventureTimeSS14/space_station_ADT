using Content.Shared.ADT.Guardians.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.ADT.Guardians.System;

/// <summary>
/// Наносит урон при выстреле. 
/// </summary>
public sealed class DamageOnShootSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnShootComponent, SelfBeforeGunShotEvent>(OnBeforeShoot);
    }

    private void OnBeforeShoot(EntityUid uid, DamageOnShootComponent comp, ref SelfBeforeGunShotEvent args)
    {
        var ent = args.Shooter;
        if (!EntityManager.EntityExists(ent))
            return;

        _damage.TryChangeDamage(ent, new DamageSpecifier { DamageDict = { [comp.DamageType] = comp.DamageAmount } });
    }
}