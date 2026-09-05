using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Ninja;
using Content.Shared.ADT.Ninja.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Climbing.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DragDrop;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.StatusEffectNew;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Ninja;

public sealed class BrainExtractorSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainExtractorConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, MapInitEvent>(OnConsoleMapInit);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, NewLinkEvent>(OnConsoleNewLink);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, PortDisconnectedEvent>(OnConsolePortDisconnected);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, PowerChangedEvent>(OnConsolePowerChanged);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChanged);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUiOpen);
        SubscribeLocalEvent<BrainExtractorConsoleComponent, BrainExtractorUiButtonPressedMessage>(OnUiButtonPressed);

        SubscribeLocalEvent<BrainExtractorPodComponent, EntInsertedIntoContainerMessage>(OnPodInserted);
        SubscribeLocalEvent<BrainExtractorPodComponent, EntRemovedFromContainerMessage>(OnPodRemoved);
        SubscribeLocalEvent<BrainExtractorPodComponent, PowerChangedEvent>(OnPodPowerChanged);
        SubscribeLocalEvent<BrainExtractorPodComponent, AnchorStateChangedEvent>(OnPodAnchorChanged);
    }

    private void OnConsoleInit(EntityUid uid, BrainExtractorConsoleComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, BrainExtractorConsoleComponent.PodPort);
    }

    private void OnConsoleMapInit(EntityUid uid, BrainExtractorConsoleComponent comp, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        foreach (var port in source.Outputs.Values.SelectMany(v => v))
        {
            if (TryComp<BrainExtractorPodComponent>(port, out var pod))
            {
                comp.ConnectedPod = port;
                pod.ConnectedConsole = uid;
                Dirty(uid, comp);
                Dirty(port, pod);
            }
        }
        RecheckConnections(uid, comp);
        UpdateUi(uid, comp);
    }

    private void OnConsoleNewLink(EntityUid uid, BrainExtractorConsoleComponent comp, NewLinkEvent args)
    {
        if (args.SourcePort != BrainExtractorConsoleComponent.PodPort)
            return;

        if (TryComp<BrainExtractorPodComponent>(args.Sink, out var pod))
        {
            comp.ConnectedPod = args.Sink;
            pod.ConnectedConsole = uid;
            Dirty(uid, comp);
            Dirty(args.Sink, pod);
        }
        RecheckConnections(uid, comp);
        UpdateUi(uid, comp);
    }

    private void OnConsolePortDisconnected(EntityUid uid, BrainExtractorConsoleComponent comp, PortDisconnectedEvent args)
    {
        if (args.Port != BrainExtractorConsoleComponent.PodPort)
            return;

        if (comp.ConnectedPod != null && TryComp<BrainExtractorPodComponent>(comp.ConnectedPod, out var pod))
        {
            pod.ConnectedConsole = null;
            Dirty(comp.ConnectedPod.Value, pod);
        }

        comp.ConnectedPod = null;
        comp.IsScanning = false;
        comp.ScanEndTime = null;
        Dirty(uid, comp);
        UpdateUi(uid, comp);
    }

    private void OnConsolePowerChanged(EntityUid uid, BrainExtractorConsoleComponent comp, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            CancelScan(uid, comp);
        }
        UpdateUi(uid, comp);
    }

    private void OnConsoleAnchorChanged(EntityUid uid, BrainExtractorConsoleComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            CancelScan(uid, comp);
        }
        else
        {
            RecheckConnections(uid, comp);
        }
        UpdateUi(uid, comp);
    }

    private void OnConsoleUiOpen(EntityUid uid, BrainExtractorConsoleComponent comp, AfterActivatableUIOpenEvent args)
    {
        UpdateUi(uid, comp);
    }

    private void OnUiButtonPressed(EntityUid uid, BrainExtractorConsoleComponent comp, BrainExtractorUiButtonPressedMessage args)
    {
        if (args.Button == BrainExtractorUiButton.StartScan)
        {
            TryStartScan(uid, comp, args.Actor);
        }
        else if (args.Button == BrainExtractorUiButton.Eject)
        {
            TryEject(uid, comp, args.Actor);
        }
    }

    private void OnPodInit(EntityUid uid, BrainExtractorPodComponent comp, ComponentInit args)
    {
        comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "brain-extractor-bodyContainer");
    }

    private void OnPodInserted(EntityUid uid, BrainExtractorPodComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != comp.BodyContainer.ID)
            return;

        if (comp.ConnectedConsole != null)
            UpdateUi(comp.ConnectedConsole.Value);

        UpdatePodAppearance(uid, comp);
    }

    private void OnPodRemoved(EntityUid uid, BrainExtractorPodComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != comp.BodyContainer.ID)
            return;

        CancelScanByPod(uid, comp);

        if (comp.ConnectedConsole != null)
            UpdateUi(comp.ConnectedConsole.Value);

        UpdatePodAppearance(uid, comp);
    }

    private void OnPodPowerChanged(EntityUid uid, BrainExtractorPodComponent comp, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelScanByPod(uid, comp);
        else
            UpdatePodAppearance(uid, comp);
    }

    private void OnPodAnchorChanged(EntityUid uid, BrainExtractorPodComponent comp, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            CancelScanByPod(uid, comp);
    }

    private void RecheckConnections(EntityUid uid, BrainExtractorConsoleComponent comp)
    {
        if (comp.ConnectedPod == null)
        {
            comp.PodInRange = false;
            return;
        }

        if (!TryComp<TransformComponent>(uid, out var xform) || !TryComp<TransformComponent>(comp.ConnectedPod, out var podXform))
        {
            comp.PodInRange = false;
            return;
        }

        var consoleCoords = xform.Coordinates;
        var podCoords = podXform.Coordinates;

        if (!consoleCoords.TryDistance(EntityManager, podCoords, out var distance))
        {
            comp.PodInRange = false;
            return;
        }

        comp.PodInRange = distance <= comp.MaxDistance;
        Dirty(uid, comp);
    }

    private void CancelScan(EntityUid consoleUid, BrainExtractorConsoleComponent comp)
    {
        if (!comp.IsScanning)
            return;

        comp.IsScanning = false;
        comp.ScanEndTime = null;
        Dirty(consoleUid, comp);

        if (comp.ConnectedPod != null && TryComp<BrainExtractorPodComponent>(comp.ConnectedPod, out var pod))
        {
            pod.IsScanning = false;
            pod.ScanEndTime = null;
            pod.ScanningNinja = null;
            Dirty(comp.ConnectedPod.Value, pod);
            UpdatePodAppearance(comp.ConnectedPod.Value, pod);
        }
        UpdateUi(consoleUid, comp);
    }

    private void CancelScanByPod(EntityUid podUid, BrainExtractorPodComponent pod)
    {
        if (!pod.IsScanning)
            return;

        pod.IsScanning = false;
        pod.ScanEndTime = null;
        pod.ScanningNinja = null;
        Dirty(podUid, pod);
        UpdatePodAppearance(podUid, pod);

        if (pod.ConnectedConsole != null && TryComp<BrainExtractorConsoleComponent>(pod.ConnectedConsole, out var console))
        {
            console.IsScanning = false;
            console.ScanEndTime = null;
            Dirty(pod.ConnectedConsole.Value, console);
            UpdateUi(pod.ConnectedConsole.Value, console);
        }
    }

    private void TryEject(EntityUid consoleUid, BrainExtractorConsoleComponent comp, EntityUid actor)
    {
        if (comp.ConnectedPod == null || !TryComp<BrainExtractorPodComponent>(comp.ConnectedPod, out var pod))
            return;

        if (pod.BodyContainer.ContainedEntity is not {} body)
            return;

        _container.Remove(body, pod.BodyContainer);
        _popup.PopupEntity(Loc.GetString("brain-extractor-eject-success"), consoleUid, actor, PopupType.Medium);
        UpdateUi(consoleUid, comp);
        UpdatePodAppearance(comp.ConnectedPod.Value, pod);
    }

    private void TryStartScan(EntityUid consoleUid, BrainExtractorConsoleComponent comp, EntityUid actor)
    {
        if (!_power.IsPowered(consoleUid))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-no-power"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (comp.ConnectedPod == null || !TryComp<BrainExtractorPodComponent>(comp.ConnectedPod, out var pod))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-no-pod"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (!comp.PodInRange)
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-pod-out-of-range"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        var podUid = comp.ConnectedPod.Value;
        if (!_power.IsPowered(podUid))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-pod-no-power"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (!Transform(podUid).Anchored || !Transform(consoleUid).Anchored)
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-not-anchored"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (comp.IsScanning || pod.IsScanning)
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-already-scanning"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (pod.BodyContainer.ContainedEntity is not {} body)
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-pod-empty"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (!_mind.TryGetMind(body, out var mindId, out _))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-no-mind"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (TerminatingOrDeleted(body) || _mobState.IsDead(body))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-target-dead"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (!_mind.TryGetObjectiveComp<BrainScanConditionComponent>(actor, out var condition))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-no-objective"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (condition.ScansCompleted >= condition.MaxScans)
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-max-scans"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        if (condition.ScannedMinds.Contains(mindId) || condition.ScannedBodies.Contains(body))
        {
            _popup.PopupEntity(Loc.GetString("brain-extractor-already-scanned"), consoleUid, actor, PopupType.MediumCaution);
            return;
        }

        var curTime = _timing.CurTime;
        pod.IsScanning = true;
        pod.ScanEndTime = curTime + pod.ScanDuration;
        pod.ScanningNinja = actor;
        comp.IsScanning = true;
        comp.ScanEndTime = pod.ScanEndTime;
        Dirty(podUid, pod);
        Dirty(consoleUid, comp);
        UpdatePodAppearance(podUid, pod);
        UpdateUi(consoleUid, comp);
        _popup.PopupEntity(Loc.GetString("brain-extractor-scan-started", ("target", body)), consoleUid, actor, PopupType.Medium);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BrainExtractorPodComponent>();
        while (query.MoveNext(out var podUid, out var pod))
        {
            if (!pod.IsScanning || pod.ScanEndTime == null)
                continue;

            if (curTime < pod.ScanEndTime)
                continue;

            if (pod.BodyContainer.ContainedEntity is not {} body)
            {
                CancelScanByPod(podUid, pod);
                continue;
            }

            if (pod.ConnectedConsole == null || !TryComp<BrainExtractorConsoleComponent>(pod.ConnectedConsole, out var console))
            {
                CancelScanByPod(podUid, pod);
                continue;
            }

            if (!_power.IsPowered(podUid) || !_power.IsPowered(pod.ConnectedConsole.Value))
            {
                CancelScanByPod(podUid, pod);
                continue;
            }

            CompleteScan(podUid, pod, console);
        }

        var consoleQuery = EntityQueryEnumerator<BrainExtractorConsoleComponent>();
        while (consoleQuery.MoveNext(out var consoleUid, out var console))
        {
            if (console.IsScanning && console.ScanEndTime != null)
            {
                var remaining = console.ScanEndTime.Value - curTime;
                if (remaining.TotalSeconds <= 0)
                    continue;

                UpdateUi(consoleUid, console);
            }
        }
    }

    private void CompleteScan(EntityUid podUid, BrainExtractorPodComponent pod, BrainExtractorConsoleComponent console)
    {
        var consoleUid = pod.ConnectedConsole!.Value;
        var body = pod.BodyContainer.ContainedEntity;
        if (body == null)
        {
            CancelScanByPod(podUid, pod);
            return;
        }

        if (!_mind.TryGetMind(body.Value, out var mindId, out _))
        {
            CancelScanByPod(podUid, pod);
            return;
        }

        var ninjaUid = pod.ScanningNinja;
        if (ninjaUid == null || TerminatingOrDeleted(ninjaUid.Value))
        {
            ninjaUid = FindNinjaForConsole(consoleUid, podUid);
        }
        if (ninjaUid == null)
        {
            if (TryComp<BrainExtractorConsoleComponent>(consoleUid, out var c))
            {
                _popup.PopupEntity(Loc.GetString("brain-extractor-scan-failed"), consoleUid, consoleUid, PopupType.MediumCaution);
            }
            CancelScanByPod(podUid, pod);
            return;
        }

        var ninja = ninjaUid.Value;
        if (!_mind.TryGetObjectiveComp<BrainScanConditionComponent>(ninja, out var condition))
        {
            CancelScanByPod(podUid, pod);
            return;
        }

        if (condition.ScannedMinds.Contains(mindId) || condition.ScannedBodies.Contains(body.Value))
        {
            CancelScanByPod(podUid, pod);
            return;
        }

        if (condition.ScansCompleted >= condition.MaxScans)
        {
            CancelScanByPod(podUid, pod);
            return;
        }

        condition.ScansCompleted++;
        Dirty(ninja, condition);
        condition.ScannedMinds.Add(mindId);
        condition.ScannedBodies.Add(body.Value);

        _statusEffects.TryAddStatusEffectDuration(body.Value, SleepingSystem.StatusEffectForcedSleeping, pod.SleepDuration);

        pod.IsScanning = false;
        pod.ScanEndTime = null;
        pod.ScanningNinja = null;
        console.IsScanning = false;
        console.ScanEndTime = null;
        Dirty(podUid, pod);
        Dirty(consoleUid, console);
        UpdatePodAppearance(podUid, pod);
        UpdateUi(consoleUid, console);

        _popup.PopupEntity(Loc.GetString("brain-extractor-scan-complete", ("target", body.Value)), consoleUid, ninja, PopupType.Large);
    }

    private EntityUid? FindNinjaForConsole(EntityUid consoleUid, EntityUid podUid)
    {
        var query = EntityQueryEnumerator<Content.Shared.Ninja.Components.SpaceNinjaComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_mind.TryGetObjectiveComp<BrainScanConditionComponent>(uid, out _))
                return uid;
        }
        return null;
    }

    private bool IsPodOccupied(BrainExtractorPodComponent comp)
    {
        return comp.BodyContainer.ContainedEntity != null;
    }

    private void UpdatePodAppearance(EntityUid podUid, BrainExtractorPodComponent pod)
    {
        if (!TryComp<AppearanceComponent>(podUid, out var appearance))
            return;
        var status = pod.IsScanning ? BrainExtractorStatus.Scanning : BrainExtractorStatus.Idle;
        _appearance.SetData(podUid, BrainExtractorVisuals.Status, status, appearance);
        if (IsPodOccupied(pod) && !pod.IsScanning)
        {
            _appearance.SetData(podUid, BrainExtractorVisuals.Status, BrainExtractorStatus.Idle, appearance);
        }
    }

    private void UpdateUi(EntityUid consoleUid, BrainExtractorConsoleComponent? comp = null)
    {
        if (!Resolve(consoleUid, ref comp))
            return;

        if (!_ui.HasUi(consoleUid, BrainExtractorUiKey.Key))
            return;

        if (!_power.IsPowered(consoleUid))
        {
            _ui.CloseUis(consoleUid);
            return;
        }

        var state = GetUiState(comp);
        _ui.SetUiState(consoleUid, BrainExtractorUiKey.Key, state);
    }

    private BrainExtractorBoundUserInterfaceState GetUiState(BrainExtractorConsoleComponent comp)
    {
        var podConnected = comp.ConnectedPod != null;
        var podInRange = comp.PodInRange;
        string? occupantName = null;
        var podOccupied = false;
        var isScanning = comp.IsScanning;
        float progress = 0f;
        var canStart = false;
        string statusText = Loc.GetString("brain-extractor-status-idle");

        if (comp.ConnectedPod != null && TryComp<BrainExtractorPodComponent>(comp.ConnectedPod, out var pod))
        {
            if (pod.BodyContainer.ContainedEntity is {} body)
            {
                occupantName = MetaData(body).EntityName;
                podOccupied = true;
            }

            if (pod.IsScanning && pod.ScanEndTime != null)
            {
                var total = (pod.ScanEndTime.Value - (pod.ScanEndTime.Value - pod.ScanDuration)).TotalSeconds;
                var remaining = (pod.ScanEndTime.Value - _timing.CurTime).TotalSeconds;
                progress = Math.Clamp(1f - (float)(remaining / total), 0f, 1f);
                isScanning = true;
                statusText = Loc.GetString("brain-extractor-status-scanning", ("progress", (int)(progress * 100)));
            }
            else if (!podOccupied)
            {
                statusText = Loc.GetString("brain-extractor-status-empty");
            }
            else if (!podConnected || !podInRange)
            {
                statusText = Loc.GetString("brain-extractor-status-no-link");
            }
            else
            {
                statusText = Loc.GetString("brain-extractor-status-ready");
                canStart = podOccupied && !isScanning && podConnected && podInRange;
            }
        }
        else
        {
            statusText = Loc.GetString("brain-extractor-status-no-pod");
        }

        if (isScanning)
            canStart = false;

        return new BrainExtractorBoundUserInterfaceState(occupantName, podConnected, podInRange, podOccupied, isScanning, progress, canStart, statusText);
    }
}
