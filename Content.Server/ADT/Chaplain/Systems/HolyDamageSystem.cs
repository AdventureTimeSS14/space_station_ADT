using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Heretic;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using System.Linq;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed class HolyDamageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly PhantomSystem _phantomSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnProjectileHit(EntityUid uid, HolyDamageComponent component, ref ProjectileHitEvent args)
    {
        if (TryComp<PhantomHolderComponent>(args.Target, out var holder))
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
            _damageableSystem.TryChangeDamage(holder.Phantom, damage);
            _phantomSystem.StopHaunt(holder.Phantom, args.Target);
        }

        if (HasComp<VesselComponent>(args.Target))
        {
            if (HasComp<PhantomPuppetComponent>(args.Target))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
            else
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }

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
        if (TryComp<PhantomHolderComponent>(args.Target, out var holder))
        {
            var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
            _damageableSystem.TryChangeDamage(holder.Phantom, damage);
            _phantomSystem.StopHaunt(holder.Phantom, args.Target);
        }

        if (HasComp<VesselComponent>(args.Target))
        {
            if (HasComp<PhantomPuppetComponent>(args.Target))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
            else
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                _damageableSystem.TryChangeDamage(args.Target, damage);
            }
        }

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
            if (HasComp<RevenantComponent>(ent) || HasComp<PhantomComponent>(ent))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
                _damageableSystem.TryChangeDamage(ent, damage);

                var time = TimeSpan.FromSeconds(2);
                _status.TryAddStatusEffect<KnockedDownComponent>(args.User, "KnockedDown", time, false);
                _status.TryAddStatusEffect<StunnedComponent>(args.User, "Stun", time, false);
            }
            if (TryComp<PhantomHolderComponent>(ent, out var holder))
            {
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), component.Damage);
                _damageableSystem.TryChangeDamage(holder.Phantom, damage);
                _phantomSystem.StopHaunt(holder.Phantom, ent);
            }

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

            if (HasComp<VesselComponent>(ent))
            {
                if (HasComp<PhantomPuppetComponent>(ent))
                {
                    var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToPuppet);
                    _damageableSystem.TryChangeDamage(ent, damage);
                }
                else
                {
                    var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Heat"), component.DamageToVessel);
                    _damageableSystem.TryChangeDamage(ent, damage);
                }
            }
        }
    }
}