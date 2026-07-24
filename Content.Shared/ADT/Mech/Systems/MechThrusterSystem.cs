using Content.Shared.Actions;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Examine;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Mech.EntitySystems;

public sealed class MechThrusterSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechThrusterComponent, MechThrusterEvent>(OnToggleThruster);
        SubscribeLocalEvent<MechThrusterComponent, InteractUsingEvent>(OnRefuel);
        SubscribeLocalEvent<MechThrusterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MechThrusterComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightless);
        SubscribeLocalEvent<MechThrusterComponent, CanWeightlessMoveEvent>(OnCanWeightlessMove);
    }

    private void OnCanWeightlessMove(EntityUid uid, MechThrusterComponent comp, ref CanWeightlessMoveEvent args)
    {
        if (comp.Active)
            args.CanMove = true;
    }

    private void OnRefreshWeightless(EntityUid uid, MechThrusterComponent comp, ref RefreshWeightlessModifiersEvent args)
    {
        if (!comp.Active)
            return;

        args.WeightlessAcceleration = comp.WeightlessAcceleration;
        args.WeightlessModifier = comp.WeightlessModifier;
        args.WeightlessFriction = comp.WeightlessFriction;
        args.WeightlessFrictionNoInput = comp.WeightlessFrictionNoInput;
    }

    private void OnToggleThruster(EntityUid uid, MechThrusterComponent comp, MechThrusterEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!comp.Active)
        {
            if (comp.Fuel <= 0f)
            {
                if (_timing.IsFirstTimePredicted)
                    _popup.PopupEntity(Loc.GetString("adt-mech-thruster-no-fuel"), uid, args.Performer);
                return;
            }

            if (TryComp<TransformComponent>(uid, out var xform) && !CanEnableOnGrid(xform.GridUid))
            {
                if (_timing.IsFirstTimePredicted)
                    _popup.PopupEntity(Loc.GetString("adt-mech-thruster-only-space"), uid, args.Performer);
                return;
            }
        }

        Toggle(uid, comp, args.Performer);
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        if (gridUid == null || !TryComp<GravityComponent>(gridUid, out var grav))
            return true;
        return !grav.Enabled;
    }

    private void Toggle(EntityUid uid, MechThrusterComponent comp, EntityUid? user = null)
    {
        comp.Active = !comp.Active;

        if (comp.Active)
        {
            EnsureComp<CanMoveInAirComponent>(uid);
            if (TryComp<PhysicsComponent>(uid, out var physics))
                _physics.SetBodyStatus(uid, physics, BodyStatus.InAir);
        }
        else
        {
            RemComp<CanMoveInAirComponent>(uid);
            if (TryComp<PhysicsComponent>(uid, out var physics))
                _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);
        }

        _movementSpeedModifier.RefreshWeightlessModifiers(uid);

        if (_netMan.IsServer)
            _audio.PlayPvs(comp.ToggleSound, uid);

        if (_timing.IsFirstTimePredicted && user != null)
        {
            var popup = comp.Active
                ? Loc.GetString("adt-mech-thruster-on")
                : Loc.GetString("adt-mech-thruster-off");
            _popup.PopupEntity(popup, uid, user.Value);
        }

        Dirty(uid, comp);
    }

    private void OnRefuel(EntityUid uid, MechThrusterComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<StackComponent>(args.Used, out var stack) || stack.StackTypeId != comp.FuelStackType)
            return;

        args.Handled = true;

        if (comp.Active)
        {
            _popup.PopupEntity(Loc.GetString("adt-mech-thruster-refuel-active"), uid, args.User);
            return;
        }

        if (comp.Fuel >= comp.MaxFuel)
        {
            _popup.PopupEntity(Loc.GetString("adt-mech-thruster-fuel-full"), uid, args.User);
            return;
        }

        if (!_netMan.IsServer)
            return;

        var needed = (int) Math.Ceiling((comp.MaxFuel - comp.Fuel) / comp.FuelPerSheet);
        var used = Math.Min(needed, stack.Count);

        if (used <= 0)
            return;

        if (!_stack.TryUse((args.Used, stack), used))
            return;

        comp.Fuel = Math.Min(comp.MaxFuel, comp.Fuel + used * comp.FuelPerSheet);
        Dirty(uid, comp);

        _popup.PopupEntity(Loc.GetString("adt-mech-thruster-refueled",
            ("fuel", (int) comp.Fuel), ("max", (int) comp.MaxFuel)), uid, args.User);
    }

    private void OnExamined(EntityUid uid, MechThrusterComponent comp, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(MechThrusterComponent)))
        {
            args.PushMarkup(Loc.GetString("adt-mech-thruster-examine",
                ("fuel", (int) comp.Fuel), ("max", (int) comp.MaxFuel)));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_netMan.IsServer)
            return;

        var query = EntityQueryEnumerator<MechThrusterComponent>();
        while (query.MoveNext(out var uid, out var thruster))
        {
            if (!thruster.Active)
                continue;

            if (TryComp<TransformComponent>(uid, out var xform) && !CanEnableOnGrid(xform.GridUid))
            {
                var pilotOnGrid = CompOrNull<MechComponent>(uid)?.PilotSlot.ContainedEntity;
                Toggle(uid, thruster, pilotOnGrid);
                continue;
            }

            thruster.Accumulator += frameTime;
            if (thruster.Accumulator < 1f)
                continue;
            thruster.Accumulator = 0f;

            thruster.Fuel = Math.Max(0f, thruster.Fuel - thruster.FuelDrain);
            Dirty(uid, thruster);

            if (thruster.Fuel > 0f)
                continue;

            var pilot = CompOrNull<MechComponent>(uid)?.PilotSlot.ContainedEntity;
            Toggle(uid, thruster, pilot);
        }
    }
}

public sealed partial class MechThrusterEvent : InstantActionEvent
{
}
