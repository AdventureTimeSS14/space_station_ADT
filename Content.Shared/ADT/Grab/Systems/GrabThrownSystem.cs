using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Grab;

public sealed class GrabThrownSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabThrownComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<GrabThrownComponent, StopThrowEvent>(OnStopThrow);
    }

    private void HandleCollide(Entity<GrabThrownComponent> ent, ref StartCollideEvent args)
    {
        if (_netMan.IsClient)
            return;

        if (!HasComp<ThrownItemComponent>(ent))
        {
            RemComp<GrabThrownComponent>(ent);
            return;
        }

        if (ent.Comp.IgnoreEntity.Contains(args.OtherEntity))
            return;

        if (!HasComp<DamageableComponent>(ent))
            RemComp<GrabThrownComponent>(ent);

        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        ent.Comp.IgnoreEntity.Add(args.OtherEntity);

        var velocitySquared = args.OurBody.LinearVelocity.LengthSquared();
        var mass = physicsComponent.Mass;
        var kineticEnergy = 0.5f * mass * velocitySquared;
        var kineticEnergyDamage = new DamageSpecifier();
        kineticEnergyDamage.DamageDict.Add("Blunt", 1);
        var modNumber = Math.Floor(kineticEnergy / 100);
        kineticEnergyDamage *= Math.Floor(modNumber / 3);
        _damageable.TryChangeDamage(args.OtherEntity, kineticEnergyDamage);
        _stamina.TakeStaminaDamage(ent, (float) Math.Floor(modNumber / 2));

        _stun.TryCrawling(args.OtherEntity);

        _color.RaiseEffect(Color.Red, new List<EntityUid>() { ent }, Filter.Pvs(ent, entityManager: EntityManager));
    }

    private void OnStopThrow(EntityUid uid, GrabThrownComponent comp, StopThrowEvent args)
    {
        if (comp.DamageOnCollide != null)
            _damageable.TryChangeDamage(uid, comp.DamageOnCollide);

        if (HasComp<GrabThrownComponent>(uid))
            RemComp<GrabThrownComponent>(uid);
    }

    public void Throw(
        EntityUid uid,
        EntityUid thrower,
        Vector2 vector,
        float grabThrownSpeed,
        DamageSpecifier? damageToUid = null,
        bool behavior = false)
    {
        var comp = EnsureComp<GrabThrownComponent>(uid);
        comp.IgnoreEntity.Add(thrower);
        comp.DamageOnCollide = damageToUid;

        _stun.TryCrawling(uid, drop: false);
        _throwing.TryThrow(uid, vector, grabThrownSpeed, animated: false);
    }
}
