using System.Numerics;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class StarGazerGrabSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;

    private const float ThrowSpeed = 14f;
    private const float SlamSpeed = 5f;
    private const float SlamDamage = 35f;
    private const float SwingDamage = 15f;
    private const float WallDamage = 150f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StarGazerComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<StarGazerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnMeleeHit(Entity<StarGazerComponent> ent, ref MeleeHitEvent args)
    {
        var gazer = ent.Owner;

        var held = GetHeld(gazer);
        if (held is not null)
        {
            Swing(gazer, held.Value, ref args);
            return;
        }

        foreach (var hit in args.HitEntities)
        {
            if (hit == gazer)
                continue;

            if (!HasComp<MobStateComponent>(hit) || _mobState.IsDead(hit))
                continue;

            if (_heretic.IsHereticOrGhoul(hit) || HasComp<StarGazerComponent>(hit))
                continue;

            args.Handled = true;
            _hands.TryForcePickupAnyHand(gazer, hit, checkActionBlocker: false);
            return;
        }
    }

    private void Swing(EntityUid gazer, EntityUid victim, ref MeleeHitEvent args)
    {
        args.Handled = true;

        if (_mobState.IsDead(victim))
        {
            _hands.TryDrop(gazer, victim, checkActionBlocker: false);
            return;
        }

        if (TryComp<DamageableComponent>(victim, out var damageable))
        {
            var spec = new DamageSpecifier();
            spec.DamageDict.Add(_proto.Index<DamageTypePrototype>("Slash"), SwingDamage);
            spec.DamageDict.Add(_proto.Index<DamageTypePrototype>("Piercing"), SwingDamage);
            _damage.TryChangeDamage((victim, damageable), spec, origin: gazer);
        }

        _stun.TryKnockdown((victim, null), TimeSpan.FromSeconds(1.5f), true);
    }

    private void OnCollide(Entity<StarGazerComponent> ent, ref StartCollideEvent args)
    {
        var gazer = ent.Owner;
        var held = GetHeld(gazer);
        if (held == null)
            return;

        var other = args.OtherEntity;
        if (other == held || HasComp<MobStateComponent>(other))
            return;

        var velocity = args.OurBody.LinearVelocity;
        if (velocity.Length() < SlamSpeed)
            return;

        var dir = velocity.Normalized();
        _hands.TryDrop(gazer, held.Value, checkActionBlocker: false);

        if (TryComp<DamageableComponent>(held, out var damageable))
        {
            var spec = new DamageSpecifier();
            spec.DamageDict.Add(_proto.Index<DamageTypePrototype>("Blunt"), SlamDamage);
            _damage.TryChangeDamage((held.Value, damageable), spec, origin: gazer);
        }

        _throw.TryThrow(held.Value, dir, ThrowSpeed, gazer, recoil: false, animated: false, doSpin: true, playSound: true);

        var impact = _xform.GetMapCoordinates(gazer).Offset(dir * 0.7f);

        foreach (var target in _lookup.GetEntitiesInRange<DamageableComponent>(impact, 0.6f))
        {
            if (target.Owner == held.Value || target.Owner == gazer)
                continue;

            var structural = new DamageSpecifier();
            structural.DamageDict.Add(_proto.Index<DamageTypePrototype>("Structural"), WallDamage);
            _damage.TryChangeDamage((target.Owner, null), structural, ignoreResistances: true, origin: gazer);
        }
    }

    private EntityUid? GetHeld(EntityUid gazer)
    {
        if (!TryComp<HandsComponent>(gazer, out var hands))
            return null;

        foreach (var handId in hands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((gazer, hands), handId, out var item))
                continue;

            if (!HasComp<MobStateComponent>(item.Value))
                continue;

            return item.Value;
        }

        return null;
    }
}