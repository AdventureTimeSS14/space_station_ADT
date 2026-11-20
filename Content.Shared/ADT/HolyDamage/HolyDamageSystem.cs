using System.Linq;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.HolyDamage;

public sealed class HolyDamageSystem : EntitySystem
{
    [Dependency] private SharedChaplainSystem _chaplain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyDamageComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<HolyDamageComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<HolyDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnProjectileHit(Entity<HolyDamageComponent> ent, ref ProjectileHitEvent args)
    {
        if (!TryComp<ProjectileComponent>(ent, out var projectile))
            return;

        _chaplain.TryDealHolyDamage(args.Target, projectile.Damage, projectile.Shooter);
    }

    private void OnThrowHit(Entity<HolyDamageComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var meleeWeapon))
            return;

        _chaplain.TryDealHolyDamage(args.Target, meleeWeapon.Damage, args.Component.Thrower);
    }

    private void OnMeleeHit(Entity<HolyDamageComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || ent.Comp.Damage <= 0f || !TryComp<MeleeWeaponComponent>(ent, out var meleeWeapon))
            return;

        foreach (var target in args.HitEntities.ToList())
        {
            _chaplain.TryDealHolyDamage(target, meleeWeapon.Damage, args.User);
        }
    }
}
