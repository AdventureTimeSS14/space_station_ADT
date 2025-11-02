using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using Content.Server.ADT.DamageBonusHoly.Components;
using System.Linq;
using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;

namespace Content.Server.ADT.DamageBonusHoly;

public sealed class DamageBonusHolySystem : EntitySystem
{
    private const string HolyDamageType = "Holy";
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageBonusHolyComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<DamageBonusHolyComponent, MeleeHitEvent>(OnMeleeHit);
    }
    private void OnProjectileHit(EntityUid uid, DamageBonusHolyComponent component, ref ProjectileHitEvent args)
    {
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            if (projectile.Damage.DamageDict.TryGetValue("Holy", out var holy))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>(HolyDamageType), holy*4);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }
    }

    private void OnMeleeHit(EntityUid uid, DamageBonusHolyComponent component, MeleeHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(uid, out var weapon))
            return;
        if (!weapon.Damage.DamageDict.TryGetValue("Holy", out var holy))
            return;
        if (!args.IsHit || !args.HitEntities.Any() || holy <= 0f)
            return;
        var damageTargets = args.HitEntities
            .Where(e => HasComp<DamageBonusHolyComponent>(e))
            .ToList();

        foreach (var ent in damageTargets)
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>(HolyDamageType), holy * 4);
            _damageableSystem.TryChangeDamage(ent, damage);
        }
    }
}
