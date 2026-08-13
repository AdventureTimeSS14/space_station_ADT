using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NPC.HTN;
using Content.Server.Speech.Components;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Xenobiology.XenobiologyControlConsole;

public sealed class XenobiologyControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    private static readonly EntProtoId[] PilotActions =
    [
        "ADTActionXenobiologyCaptureSlime",
        "ADTActionXenobiologyPlaceSlime",
        "ADTActionXenobiologyFeedMonkey",
        "ADTActionXenobiologyRecycleMonkey",
        "ADTActionXenobiologyReturn",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyControlConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, ExaminedEvent>(OnConsoleExamined);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        SubscribeLocalEvent<XenobiologyEyeComponent, ComponentShutdown>(OnEyeShutdown);

        SubscribeLocalEvent<XenobiologyEyePilotComponent, MobStateChangedEvent>(OnPilotMobStateChanged);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, InteractionAttemptEvent>(OnPilotInteractionAttempt);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, XenobiologyCaptureSlimeEvent>(OnCaptureSlime);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, XenobiologyPlaceSlimeEvent>(OnPlaceSlime);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, XenobiologyFeedMonkeyEvent>(OnFeedMonkey);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, XenobiologyRecycleMonkeyEvent>(OnRecycleMonkey);
        SubscribeLocalEvent<XenobiologyEyePilotComponent, XenobiologyReturnEvent>(OnReturn);
    }

    private void OnMapInit(Entity<XenobiologyControlConsoleComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, XenobiologyControlConsoleComponent.SlimeContainerId);
    }

    private void OnConsoleShutdown(Entity<XenobiologyControlConsoleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Pilot is { } pilot)
            ExitPilotMode(ent, pilot);
    }

    private void OnEyeShutdown(Entity<XenobiologyEyeComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) || console.Pilot != ent.Comp.Pilot)
            return;

        ExitPilotMode((ent.Comp.Console, console), ent.Comp.Pilot, deleteEye: false);
    }

    private void OnPilotMobStateChanged(Entity<XenobiologyEyePilotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            ExitPilotMode((ent.Comp.Console, console), ent.Owner);
    }

    private void OnPilotInteractionAttempt(Entity<XenobiologyEyePilotComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Target != null)
            args.Cancelled = true;
    }

    private void OnActivate(Entity<XenobiologyControlConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;

        if (ent.Comp.Pilot == args.User)
        {
            ExitPilotMode(ent, args.User);
            return;
        }

        if (ent.Comp.Pilot != null)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-in-use"), ent, args.User);
            return;
        }

        if (TryComp<AccessReaderComponent>(ent, out var access) && !_access.IsAllowed(args.User, ent, access))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-access-denied"), ent, args.User);
            return;
        }

        EnterPilotMode(ent, args.User);
    }

    private void EnterPilotMode(Entity<XenobiologyControlConsoleComponent> ent, EntityUid user)
    {
        var xform = Transform(ent);
        var spawnCoords = xform.Coordinates;
        if (_area.TryGetAreaCenter(ent.Comp.Area, xform.GridUid ?? EntityUid.Invalid, out var zoneCenter))
            spawnCoords = zoneCenter;

        var eye = Spawn(ent.Comp.EyeProto, spawnCoords);
        AddComp(eye, new XenobiologyEyeComponent { Pilot = user, Console = ent, AllowedArea = ent.Comp.Area });

        if (TryComp<EyeComponent>(user, out var eyeComp))
        {
            _eye.SetDrawFov(user, false, eyeComp);
            _eye.SetTarget(user, eye, eyeComp);
        }

        _mover.SetRelay(user, eye);

        var overlay = EnsureComp<StationAiOverlayComponent>(user);
        overlay.VisionNetwork = ent.Comp.Area;

        var pilot = AddComp<XenobiologyEyePilotComponent>(user);
        pilot.Console = ent;
        pilot.Eye = eye;

        foreach (var actionProto in PilotActions)
        {
            pilot.Actions[actionProto] = _actions.AddAction(user, actionProto);
        }

        ent.Comp.Pilot = user;
        ent.Comp.Eye = eye;

        _popup.PopupEntity(Loc.GetString("xenobiology-control-console-enter"), ent, user);
    }

    private void ExitPilotMode(Entity<XenobiologyControlConsoleComponent> ent, EntityUid user, bool deleteEye = true)
    {
        if (TryComp<EyeComponent>(user, out var eyeComp))
        {
            _eye.SetTarget(user, null, eyeComp);
            _eye.SetDrawFov(user, true, eyeComp);
        }

        RemCompDeferred<RelayInputMoverComponent>(user);
        RemComp<StationAiOverlayComponent>(user);

        if (TryComp<XenobiologyEyePilotComponent>(user, out var pilot))
        {
            foreach (var action in pilot.Actions.Values)
            {
                _actions.RemoveAction(user, action);
            }
        }

        RemComp<XenobiologyEyePilotComponent>(user);

        if (deleteEye)
            QueueDel(ent.Comp.Eye);

        ent.Comp.Pilot = null;
        ent.Comp.Eye = null;

        _popup.PopupEntity(Loc.GetString("xenobiology-control-console-exit"), ent, user);
    }

    private void OnReturn(Entity<XenobiologyEyePilotComponent> ent, ref XenobiologyReturnEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        ExitPilotMode((ent.Comp.Console, console), ent);
        args.Handled = true;
    }

    private void OnCaptureSlime(Entity<XenobiologyEyePilotComponent> ent, ref XenobiologyCaptureSlimeEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !_container.TryGetContainer(ent.Comp.Console, XenobiologyControlConsoleComponent.SlimeContainerId, out var container))
            return;

        if (container.ContainedEntities.Count >= console.MaxSlimeCapacity)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-slime-storage-full"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        var target = _lookup.GetEntitiesInRange<SlimeComponent>(eyeCoords, console.InteractRange)
            .Select(e => e.Owner)
            .FirstOrDefault(candidate => !_mobState.IsDead(candidate) && !container.Contains(candidate));

        if (target == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-slime"), ent.Comp.Eye, ent);
            return;
        }

        if (TryComp<HTNComponent>(target, out var htn))
            _htn.SetHTNEnabled((target, htn), false);

        if (!_container.Insert(target, container))
            return;

        _audio.PlayPvs(console.SuctionSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnPlaceSlime(Entity<XenobiologyEyePilotComponent> ent, ref XenobiologyPlaceSlimeEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !_container.TryGetContainer(ent.Comp.Console, XenobiologyControlConsoleComponent.SlimeContainerId, out var container))
            return;

        var slime = container.ContainedEntities.FirstOrDefault();

        if (slime == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-slime-storage-empty"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        _container.Remove(slime, container);
        _xform.SetCoordinates(slime, eyeCoords);

        if (TryComp<HTNComponent>(slime, out var htn))
            _htn.SetHTNEnabled((slime, htn), true, 2f);

        _audio.PlayPvs(console.EjectSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnFeedMonkey(Entity<XenobiologyEyePilotComponent> ent, ref XenobiologyFeedMonkeyEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !TryComp<StorageComponent>(ent.Comp.Console, out var storage))
            return;

        var cube = storage.Container.ContainedEntities.FirstOrDefault();

        if (cube == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-monkey-stock"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        _container.Remove(cube, storage.Container);
        QueueDel(cube);
        Spawn("MobMonkey", eyeCoords);

        _audio.PlayPvs(console.EjectSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnRecycleMonkey(Entity<XenobiologyEyePilotComponent> ent, ref XenobiologyRecycleMonkeyEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        if (!TryGetLinkedRecycler(ent.Comp.Console, out _, out var recycler))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-recycler"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        var target = _lookup.GetEntitiesInRange(eyeCoords, console.InteractRange)
            .FirstOrDefault(candidate => HasComp<MonkeyAccentComponent>(candidate) && _mobState.IsDead(candidate));

        if (target == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-dead-monkey"), ent.Comp.Eye, ent);
            return;
        }

        for (var i = 0; i < recycler.CubeProduction; i++)
        {
            var newCube = Spawn("MonkeyCube", Transform(ent.Comp.Console).Coordinates);

            if (!_storage.Insert(ent.Comp.Console, newCube, out _))
            {
                QueueDel(newCube);
                break;
            }
        }

        QueueDel(target);

        _audio.PlayPvs(console.SuctionSound, ent.Comp.Console);
        args.Handled = true;
    }

    private bool TryGetLinkedRecycler(EntityUid console, out EntityUid recycler, [NotNullWhen(true)] out XenobiologyMonkeyRecyclerComponent? recyclerComp)
    {
        var query = EntityQueryEnumerator<XenobiologyMonkeyRecyclerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_deviceLink.GetLinks(uid, console).Count > 0)
            {
                recycler = uid;
                recyclerComp = comp;
                return true;
            }
        }

        recycler = default;
        recyclerComp = null;
        return false;
    }

    private void OnConsoleExamined(Entity<XenobiologyControlConsoleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<StorageComponent>(ent, out var storage))
            return;

        args.PushMarkup(Loc.GetString("xenobiology-control-console-examine-cubes",
            ("count", storage.Container.ContainedEntities.Count)));
    }
}
