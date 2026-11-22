using Content.Shared.Actions;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Mech.EntitySystems;

public sealed class MechOverloadSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechOverloadComponent, MechOverloadEvent>(OnToggleOverload);
        SubscribeLocalEvent<MechOverloadComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<MechOverloadComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<MechOverloadComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
    }

    private void OnToggleOverload(Entity<MechOverloadComponent> ent, ref MechOverloadEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<MechComponent>(ent, out var mech))
            return;

        if (!ent.Comp.Overload && mech.Integrity <= ent.Comp.MinIng)
        {
            _popup.PopupClient(Loc.GetString("adt-mech-overload-too-damaged"), ent, args.Performer);
            return;
        }

        SetOverload(ent, !ent.Comp.Overload);

        if (ent.Comp.Overload && _net.IsServer)
        {
            Spawn("EffectSparks", Transform(ent).Coordinates);
            _damageable.TryChangeDamage(ent.Owner, ent.Comp.DamagePerSpeed, ignoreResistances: true);
        }
    }

    private void SetOverload(Entity<MechOverloadComponent> ent, bool value)
    {
        ent.Comp.Overload = value;
        ent.Comp.NextDamage = _timing.CurTime + ent.Comp.DamageInterval;
        Dirty(ent);

        _actions.SetToggled(ent.Comp.MechOverloadActionEntity, value);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
        _movementSpeedModifier.RefreshFrictionModifiers(ent.Owner);
    }

    private void OnDamage(Entity<MechOverloadComponent> ent, ref DamageChangedEvent args)
    {
        if (!ent.Comp.Overload || !TryComp<MechComponent>(ent, out var mech))
            return;

        if (mech.Integrity > ent.Comp.MinIng)
            return;

        SetOverload(ent, false);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<MechOverloadComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Overload)
            args.ModifySpeed(ent.Comp.WalkSpeedMultiplier, ent.Comp.SprintSpeedMultiplier);
    }

    private void OnRefreshFrictionModifiers(Entity<MechOverloadComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        if (ent.Comp.Overload)
            args.ModifyAcceleration(ent.Comp.AccelerationMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MechOverloadComponent>();
        while (query.MoveNext(out var uid, out var overload))
        {
            if (!overload.Overload || overload.NextDamage > now)
                continue;

            overload.NextDamage = now + overload.DamageInterval;
            Dirty(uid, overload);

            _damageable.TryChangeDamage(uid, overload.DamagePerSpeed, ignoreResistances: true, interruptsDoAfters: false);
        }
    }
}

public sealed partial class MechOverloadEvent : InstantActionEvent
{
}
