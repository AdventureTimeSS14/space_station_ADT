using Content.Shared.ADT.Body.Limbs;
using Content.Shared.ADT.Silicon;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Body.Prosthesis;

public sealed class ProsthesisSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProsthesisComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<ProsthesisComponent, OrganGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<ProsthesisComponent, BodyRelayedEvent<EmpPulseEvent>>(OnEmpPulse);
        SubscribeLocalEvent<ProsthesisUserComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ProsthesisUserComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGotInserted(Entity<ProsthesisComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!HasComp<MovementSpeedModifierComponent>(args.Target))
            return;

        EnsureComp<ProsthesisUserComponent>(args.Target);
        _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnGotRemoved(Entity<ProsthesisComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating || !HasComp<ProsthesisUserComponent>(args.Target))
            return;

        _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnRefreshSpeed(Entity<ProsthesisUserComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<ProsthesisComponent>(ent.Owner))
        {
            args.ModifySpeed(organ.Comp2.SpeedModifier);
        }
    }

    private void OnGetMeleeDamage(Entity<ProsthesisUserComponent> ent, ref GetMeleeDamageEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<ProsthesisComponent>(ent.Owner))
        {
            args.Damage *= organ.Comp2.MeleeModifier;
        }
    }

    private void OnEmpPulse(Entity<ProsthesisComponent> ent, ref BodyRelayedEvent<EmpPulseEvent> args)
    {
        var body = args.Body.Owner;

        if (HasComp<MobIpcComponent>(body))
            return;

        _damageable.TryChangeDamage(body, ent.Comp.EmpDamage, ignoreResistances: true, interruptsDoAfters: false);

        if (TryComp<LimbOrganComponent>(ent, out var limb))
        {
            if ((limb.Part & LimbParts.Legs) != 0)
                _stun.TryKnockdown(body, ent.Comp.EmpKnockdown);

            if (TryComp<HandOrganComponent>(ent, out var hand))
                _hands.TryDrop(body, hand.HandID, checkActionBlocker: false);
        }

        var ev = args.Args;
        ev.Affected = true;
        args.Args = ev;
    }
}
