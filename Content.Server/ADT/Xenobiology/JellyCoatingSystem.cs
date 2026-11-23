using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology;

[Virtual]
public partial class JellyCoatingSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextCheck;
    private readonly List<EntityUid> _expired = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellyCoatingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SpeedBoostedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SpeedBoostedComponent, ComponentRemove>(OnBoostRemoved);
        SubscribeLocalEvent<MechPilotComponent, RefreshMovementSpeedModifiersEvent>(OnPilotRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextCheck)
            return;

        _nextCheck = _timing.CurTime + CheckInterval;

        _expired.Clear();
        var query = EntityQueryEnumerator<SpeedBoostedComponent>();
        while (query.MoveNext(out var uid, out var boosted))
        {
            if (_timing.CurTime >= boosted.EndTime)
                _expired.Add(uid);
        }

        foreach (var uid in _expired)
        {
            RemComp<SpeedBoostedComponent>(uid);
            _popup.PopupEntity(Loc.GetString("jelly-coating-expired", ("target", uid)), uid);
        }
    }

    private void OnAfterInteract(Entity<JellyCoatingComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        if (TryComp<MechPilotComponent>(target, out var pilot))
            target = pilot.Mech;

        if (HasComp<HumanoidProfileComponent>(target))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(target, out var _))
            return;

        var boosted = EnsureComp<SpeedBoostedComponent>(target);
        boosted.SpeedMultiplier = ent.Comp.SpeedMultiplier;
        boosted.EndTime = _timing.CurTime + ent.Comp.Duration;

        _popup.PopupEntity(Loc.GetString("jelly-coating-success",
            ("target", target),
            ("source", ent.Owner),
            ("minutes", (int) ent.Comp.Duration.TotalMinutes)), target);

        Dirty(target, boosted);
        RefreshSpeed(target);

        _entityManager.PredictedQueueDeleteEntity(ent.Owner);
        args.Handled = true;
    }

    private void OnBoostRemoved(Entity<SpeedBoostedComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ent.Comp.SpeedMultiplier = 1f;
        RefreshSpeed(ent);
    }

    private void RefreshSpeed(EntityUid target)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(target);

        if (!HasComp<MechComponent>(target))
            return;

        var pilots = EntityQueryEnumerator<MechPilotComponent>();
        while (pilots.MoveNext(out var pilot, out var pilotComp))
        {
            if (pilotComp.Mech == target)
                _movementSpeedModifier.RefreshMovementSpeedModifiers(pilot);
        }
    }

    private void OnRefreshSpeed(EntityUid uid, SpeedBoostedComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedMultiplier);
    }

    private void OnPilotRefreshSpeed(EntityUid uid, MechPilotComponent pilot, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!HasComp<MechComponent>(pilot.Mech))
            return;

        if (!TryComp<SpeedBoostedComponent>(pilot.Mech, out var boosted))
            return;

        args.ModifySpeed(boosted.SpeedMultiplier);
    }
}
