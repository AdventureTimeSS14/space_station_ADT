using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.EyeControl;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.EyeControl;

public sealed class EyeControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private static readonly EntProtoId ReturnAction = "ADTActionEyeControlReturn";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeControlConsoleComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<EyeControlConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        SubscribeLocalEvent<EyeControlEyeComponent, ComponentShutdown>(OnEyeShutdown);

        SubscribeLocalEvent<EyeControlPilotComponent, MobStateChangedEvent>(OnPilotMobStateChanged);
        SubscribeLocalEvent<EyeControlPilotComponent, InteractionAttemptEvent>(OnPilotInteractionAttempt);
        SubscribeLocalEvent<EyeControlPilotComponent, EyeControlReturnEvent>(OnReturn);
    }

    private void OnConsoleShutdown(Entity<EyeControlConsoleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Pilot is { } pilot)
            ExitPilotMode(ent, pilot);
    }

    private void OnEyeShutdown(Entity<EyeControlEyeComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<EyeControlConsoleComponent>(ent.Comp.Console, out var console) || console.Pilot != ent.Comp.Pilot)
            return;

        ExitPilotMode((ent.Comp.Console, console), ent.Comp.Pilot, deleteEye: false);
    }

    private void OnPilotMobStateChanged(Entity<EyeControlPilotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && TryComp<EyeControlConsoleComponent>(ent.Comp.Console, out var console))
            ExitPilotMode((ent.Comp.Console, console), ent.Owner);
    }

    private void OnPilotInteractionAttempt(Entity<EyeControlPilotComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Target != null)
            args.Cancelled = true;
    }

    private void OnActivate(Entity<EyeControlConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;

        if (HasComp<StationAiHeldComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("eye-control-console-ai-denied"), ent, args.User);
            return;
        }

        if (ent.Comp.Pilot == args.User)
        {
            ExitPilotMode(ent, args.User);
            return;
        }

        if (ent.Comp.Pilot != null)
        {
            _popup.PopupEntity(Loc.GetString("eye-control-console-in-use"), ent, args.User);
            return;
        }

        if (TryComp<AccessReaderComponent>(ent, out var access) && !_access.IsAllowed(args.User, ent, access))
        {
            _popup.PopupEntity(Loc.GetString("eye-control-console-access-denied"), ent, args.User);
            return;
        }

        EnterPilotMode(ent, args.User);
    }

    private void EnterPilotMode(Entity<EyeControlConsoleComponent> ent, EntityUid user)
    {
        var spawnCoords = GetEyeSpawnCoords(ent);

        var eye = Spawn(ent.Comp.EyeProto, spawnCoords);
        AddComp(eye, new EyeControlEyeComponent { Pilot = user, Console = ent, AllowedArea = ent.Comp.Area });

        if (TryComp<EyeComponent>(user, out var eyeComp))
        {
            _eye.SetDrawFov(user, false, eyeComp);
            _eye.SetTarget(user, eye, eyeComp);
        }

        _mover.SetRelay(user, eye);

        var overlay = EnsureComp<StationAiOverlayComponent>(user);
        overlay.VisionNetwork = ent.Comp.VisionNetwork ?? ent.Comp.Area?.Id;

        if (TryComp<ExaminerComponent>(user, out var examiner))
            examiner.SkipChecks = true;

        var pilot = AddComp<EyeControlPilotComponent>(user);
        pilot.Console = ent;
        pilot.Eye = eye;

        if (TryComp<InputMoverComponent>(user, out var mover))
        {
            pilot.PreviousRelativeRotation = mover.RelativeRotation;
            pilot.PreviousTargetRelativeRotation = mover.TargetRelativeRotation;

            var bodyRot = GetRelativeRotation(mover);
            var eyeXform = Transform(eye);
            var eyeRot = (eyeXform.GridUid ?? eyeXform.MapUid) is { } eyeRelative
                ? _xform.GetWorldRotation(eyeRelative)
                : Angle.Zero;

            var correction = eyeRot - bodyRot;
            mover.RelativeRotation = correction;
            mover.TargetRelativeRotation = correction;
            Dirty(user, mover);
        }

        pilot.Actions[ReturnAction] = _actions.AddAction(user, ReturnAction);
        foreach (var actionProto in ent.Comp.Actions)
        {
            if (pilot.Actions.ContainsKey(actionProto))
                continue;

            pilot.Actions[actionProto] = _actions.AddAction(user, actionProto);
        }

        ent.Comp.Pilot = user;
        ent.Comp.Eye = eye;

        _popup.PopupEntity(Loc.GetString("eye-control-console-enter"), ent, user);
    }

    private EntityCoordinates GetEyeSpawnCoords(Entity<EyeControlConsoleComponent> ent)
    {
        var xform = Transform(ent);
        var spawnCoords = xform.Coordinates;

        if (ent.Comp.Area is { } area)
        {
            if (_area.TryGetAreaCenter(area, xform.GridUid ?? EntityUid.Invalid, out var zoneCenter))
                spawnCoords = zoneCenter;

            return spawnCoords;
        }

        var station = _station.GetStations().FirstOrDefault();
        if (station != default && TryComp<StationDataComponent>(station, out var data))
        {
            var grid = data.Grids.FirstOrNull(HasComp<BecomesStationComponent>) ?? _station.GetLargestGrid(station);

            if (grid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var mapGrid))
            {
                var centreTile = new Vector2i((int)mapGrid.LocalAABB.Center.X, (int)mapGrid.LocalAABB.Center.Y);
                spawnCoords = _map.GridTileToLocal(gridUid, mapGrid, centreTile);
            }
        }

        return spawnCoords;
    }

    private void ExitPilotMode(Entity<EyeControlConsoleComponent> ent, EntityUid user, bool deleteEye = true)
    {
        if (TryComp<EyeComponent>(user, out var eyeComp))
        {
            _eye.SetTarget(user, null, eyeComp);
            _eye.SetDrawFov(user, true, eyeComp);
        }

        RemCompDeferred<RelayInputMoverComponent>(user);
        RemComp<StationAiOverlayComponent>(user);

        if (TryComp<ExaminerComponent>(user, out var examiner))
            examiner.SkipChecks = false;

        if (TryComp<EyeControlPilotComponent>(user, out var pilot))
        {
            foreach (var action in pilot.Actions.Values)
            {
                _actions.RemoveAction(user, action);
            }

            if (TryComp<InputMoverComponent>(user, out var mover))
            {
                mover.RelativeRotation = pilot.PreviousRelativeRotation;
                mover.TargetRelativeRotation = pilot.PreviousTargetRelativeRotation;
                Dirty(user, mover);
            }
        }

        RemComp<EyeControlPilotComponent>(user);

        if (deleteEye)
            QueueDel(ent.Comp.Eye);

        ent.Comp.Pilot = null;
        ent.Comp.Eye = null;

        _popup.PopupEntity(Loc.GetString("eye-control-console-exit"), ent, user);
    }

    private void OnReturn(Entity<EyeControlPilotComponent> ent, ref EyeControlReturnEvent args)
    {
        if (!TryComp<EyeControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        ExitPilotMode((ent.Comp.Console, console), ent);
        args.Handled = true;
    }

    private Angle GetRelativeRotation(InputMoverComponent mover)
    {
        if (mover.RelativeEntity is { Valid: true } relative && TryComp<TransformComponent>(relative, out var relXform))
            return _xform.GetWorldRotation(relXform);

        return Angle.Zero;
    }
}
