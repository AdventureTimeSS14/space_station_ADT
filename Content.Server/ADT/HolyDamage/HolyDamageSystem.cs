using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using System.Linq;
using Content.Shared.Damage.Systems;

namespace Content.Server.ADT.HolyDamage;

public sealed class HolyDamageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    private const int MinimumPathStage = 5;
    private const float DamageIncreasePerStage = 0.1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnProjectileHit(EntityUid uid, HolyDamageComponent component, ref ProjectileHitEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.Target, out var heretic, out _) ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            !projectile.Damage.DamageDict.TryGetValue("Holy", out var holyDamage))
        {
            return;
        }

        ApplyHolyDamage(args.Target, heretic.PathStage, holyDamage);
    }

    private void OnThrowHit(EntityUid uid, HolyDamageComponent component, ThrowDoHitEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.Target, out var heretic, out _) ||
            !TryComp<MeleeWeaponComponent>(uid, out var meleeWeapon) ||
            !meleeWeapon.Damage.DamageDict.TryGetValue("Holy", out var holyDamage))
        {
            return;
        }

        ApplyHolyDamage(args.Target, heretic.PathStage, holyDamage);
    }

    private void OnMeleeHit(EntityUid uid, HolyDamageComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            component.Damage <= 0f ||
            !TryComp<MeleeWeaponComponent>(uid, out var meleeWeapon) ||
            !meleeWeapon.Damage.DamageDict.TryGetValue("Holy", out var holyDamage))
        {
            return;
        }

        foreach (var target in args.HitEntities)
        {
            if (!_heretic.TryGetHereticComponent(target, out var heretic, out _))
                continue;

            ApplyHolyDamage(target, heretic.PathStage, holyDamage);
        }
    }

    private void ApplyHolyDamage(EntityUid target, int pathStage, FixedPoint2 holyDamage)
    {
        if (pathStage < MinimumPathStage || holyDamage <= FixedPoint2.Zero)
            return;

        var damageMultiplier = 1f + (pathStage - MinimumPathStage) * DamageIncreasePerStage;
        var thermalDamage = (float)holyDamage * damageMultiplier;

        var damage = new DamageSpecifier(
            _proto.Index<DamageTypePrototype>("Heat"),
            FixedPoint2.New(thermalDamage));

        _damageableSystem.TryChangeDamage(
            target,
            damage,
            ignoreResistances: true);
    }
}
