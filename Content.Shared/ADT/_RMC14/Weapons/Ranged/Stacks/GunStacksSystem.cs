﻿using Content.Shared._RMC14.Armor;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

public sealed class GunStacksSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCSelectiveFireSystem _rmcSelectiveFire = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<GunStacksComponent> _gunStacksQuery;
    private EntityQuery<RMCSelectiveFireComponent> _selectiveFireQuery;

    public override void Initialize()
    {
        _gunStacksQuery = GetEntityQuery<GunStacksComponent>();
        _selectiveFireQuery = GetEntityQuery<RMCSelectiveFireComponent>();

        SubscribeLocalEvent<GunStacksComponent, GetGunDamageModifierEvent>(OnStacksGetGunDamageModifier);
        SubscribeLocalEvent<GunStacksComponent, GunGetFireRateEvent>(OnStacksGetGunFireRate);
        SubscribeLocalEvent<GunStacksComponent, AmmoShotEvent>(OnStacksAmmoShot);
        SubscribeLocalEvent<GunStacksComponent, DroppedEvent>(OnStacksDropped);

        SubscribeLocalEvent<GunStacksProjectileComponent, ProjectileHitEvent>(OnStacksProjectileHit);
    }

    private void OnStacksGetGunDamageModifier(Entity<GunStacksComponent> ent, ref GetGunDamageModifierEvent args)
    {
        if (ent.Comp.Hits > 0)
            args.Multiplier += ent.Comp.DamageIncrease;
    }

    private void OnStacksGetGunFireRate(Entity<GunStacksComponent> ent, ref GunGetFireRateEvent args)
    {
        if (ent.Comp.Hits > 0)
            args.FireRate = ent.Comp.SetFireRate;
    }

    private void OnStacksAmmoShot(Entity<GunStacksComponent> ent, ref AmmoShotEvent args)
    {
        var time = _timing.CurTime;
        if (ent.Comp.Hits > 0 && time >= ent.Comp.LastHitAt + ent.Comp.StacksExpire)
        {
            ent.Comp.Hits = 0;
            Dirty(ent);
        }

        foreach (var bullet in args.FiredProjectiles)
        {
            var stacks = EnsureComp<GunStacksProjectileComponent>(bullet);
            stacks.Gun = ent;
            Dirty(bullet, stacks);

            var ap = Math.Min(ent.Comp.MaxAP, ent.Comp.IncreaseAP * ent.Comp.Hits);
        }
    }

    private void OnStacksDropped(Entity<GunStacksComponent> ent, ref DroppedEvent args)
    {
        Reset(ent);
    }

    private void OnStacksProjectileHit(Entity<GunStacksProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_gunStacksQuery.TryComp(ent.Comp.Gun, out var gun))
            return;

        if (TryComp(ent, out ProjectileComponent? projectile) &&
            projectile.DamagedEntity)
        {
            return;
        }

        var target = args.Target;


        if (_selectiveFireQuery.TryComp(ent.Comp.Gun, out var selective))
            _rmcSelectiveFire.RefreshFireModeGunValues((ent.Comp.Gun.Value, selective));

        Dirty(ent);
    }

    private void Reset(Entity<GunStacksComponent> gun)
    {
        gun.Comp.Hits = 0;
        Dirty(gun);
    }
}
