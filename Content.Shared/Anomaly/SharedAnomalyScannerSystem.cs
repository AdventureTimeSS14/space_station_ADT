using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory; // ADT-Tweak
using Content.Shared.Popups;
using Content.Shared.Verbs; // ADT-Tweak
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Anomaly;

/// <summary> System for controlling anomaly scanner device. </summary>
public abstract class SharedAnomalyScannerSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyScannerComponent, ScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnScannerAnomalyShutdown);
        SubscribeLocalEvent<AnomalyScannerComponent, InventoryRelayedEvent<GetVerbsEvent<InnateVerb>>>(AddScanVerb);  // ADT-Tweak
    }

    // ADT-Tweak-start
    private void AddScanVerb(Entity<AnomalyScannerComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!args.Args.CanInteract || !args.Args.CanAccess || !HasComp<AnomalyComponent>(args.Args.Target))
            return;

        var target = args.Args.Target;
        var user = args.Args.User;

        var verb = new InnateVerb
        {
            Act = () => StartScan(ent, user, target),
            Text = Loc.GetString("anomaly-scanner-verb-scan"),
            IconEntity = GetNetEntity(ent),
            Priority = 2,
        };
        args.Args.Verbs.Add(verb);
    }

    private void StartScan(Entity<AnomalyScannerComponent> ent, EntityUid user, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.ScanDoAfterDuration,
            new ScannerDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner
        )
        {
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }
    // ADT-Tweak-end

    private void OnScannerAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UI.CloseUi(uid, AnomalyScannerUiKey.Key);
            // Anomaly over, reset all the appearance data
            Appearance.SetData(uid, AnomalyScannerVisuals.HasAnomaly, false);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyIsSupercritical, false);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyNextPulse, 0);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, 0);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, AnomalyStabilityVisuals.Stable);
        }
    }

    private void OnScannerAfterInteract(EntityUid uid, AnomalyScannerComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!HasComp<AnomalyComponent>(target))
            return;

        if (!args.CanReach)
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            component.ScanDoAfterDuration,
            new ScannerDoAfterEvent(),
            uid,
            target: target,
            used: uid
        )
        {
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    protected virtual void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        Audio.PlayPredicted(component.CompleteSound, uid, args.User);
        Popup.PopupPredicted(Loc.GetString("anomaly-scanner-component-scan-complete"), uid, args.User);

        UI.OpenUi(uid, AnomalyScannerUiKey.Key, args.User);

        args.Handled = true;
    }

}
