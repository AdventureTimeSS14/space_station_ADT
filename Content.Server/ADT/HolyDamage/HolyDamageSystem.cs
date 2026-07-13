using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Heretic;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using System.Linq;
using Content.Shared.Damage.Systems;

namespace Content.Server.ADT.HolyDamage;

public sealed class HolyDamageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnProjectileHit(EntityUid uid, HolyDamageComponent component, ref ProjectileHitEvent args)
    {
        if (TryComp<HereticComponent>(args.Target, out var heretic) &&
            TryComp<ProjectileComponent>(uid, out var projectile))
        {
            if (projectile.Damage.DamageDict.TryGetValue("Holy", out var holyDamage))
            {
                var thermalDamage = 0.1428f * heretic.PathStage * (float)holyDamage;
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), FixedPoint2.New(thermalDamage));
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }
    }

    private void OnThrowHit(EntityUid uid, HolyDamageComponent component, ThrowDoHitEvent args)
    {
        if (TryComp<HereticComponent>(args.Target, out var heretic))
        {
            FixedPoint2 holyDamage = FixedPoint2.Zero;

            if (TryComp<MeleeWeaponComponent>(uid, out var meleeWeapon) &&
                meleeWeapon.Damage.DamageDict.TryGetValue("Holy", out var meleeHolyDamage))
            {
                holyDamage = meleeHolyDamage;
            }

            if (holyDamage > FixedPoint2.Zero)
            {
                var thermalDamage = 0.1428f * heretic.PathStage * (float)holyDamage;
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), FixedPoint2.New(thermalDamage));
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }
    }

    private void OnMeleeHit(EntityUid uid, HolyDamageComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            component.Damage <= 0f)
        {
            return;
        }

        foreach (var ent in args.HitEntities)
        {
            if (TryComp<HereticComponent>(ent, out var heretic))
            {
                if (TryComp<MeleeWeaponComponent>(uid, out var meleeWeapon))
                {
                    if (meleeWeapon.Damage.DamageDict.TryGetValue("Holy", out var holyDamage))
                    {
                        var thermalDamage = 0.1428f * heretic.PathStage * (float)holyDamage;
                        var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), FixedPoint2.New(thermalDamage));
                        _damageableSystem.TryChangeDamage(ent, damage);
                    }
                }
            }
        }
    }
}
