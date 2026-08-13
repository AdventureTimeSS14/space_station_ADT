using System.Linq;
using System.Numerics;
using Content.Shared.ADT.GPS;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.GPS;

public sealed class GpsSystem : SharedGpsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const int CarrierSearchDepth = 8;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GpsComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<GpsComponent, GpsToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<GpsComponent, GpsToggleRangeMessage>(OnToggleRangeMessage);
        SubscribeLocalEvent<GpsComponent, GpsToggleSosMessage>(OnToggleSosMessage);
        SubscribeLocalEvent<GpsComponent, GpsSetTagMessage>(OnSetTagMessage);
        SubscribeLocalEvent<GpsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<GpsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_ui.IsUiOpen(uid, GpsUiKey.Key))
                continue;

            CloseForInvalidActors(uid);

            if (curTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = curTime + TimeSpan.FromSeconds(comp.UpdateRate);

            if (!_ui.IsUiOpen(uid, GpsUiKey.Key))
                continue;

            UpdateUiState((uid, comp));
        }
    }

    private void OnUiOpened(Entity<GpsComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnToggleMessage(Entity<GpsComponent> ent, ref GpsToggleMessage args)
    {
        TryToggle(ent, args.Actor);
    }

    private void OnToggleRangeMessage(Entity<GpsComponent> ent, ref GpsToggleRangeMessage args)
    {
        SetSameMapOnly(ent, !ent.Comp.SameMapOnly);
    }

    private void OnToggleSosMessage(Entity<GpsComponent> ent, ref GpsToggleSosMessage args)
    {
        TryToggleSos(ent, args.Actor);
    }

    private void OnSetTagMessage(Entity<GpsComponent> ent, ref GpsSetTagMessage args)
    {
        SetTag(ent, args.Tag);
    }

    private void OnUiOpenAttempt(Entity<GpsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || IsCarriedBy(ent.Owner, args.User))
            return;

        args.Cancel();
    }

    private void CloseForInvalidActors(EntityUid uid)
    {
        foreach (var actor in _ui.GetActors(uid, GpsUiKey.Key).ToArray())
        {
            if (IsCarriedBy(uid, actor))
                continue;

            _ui.CloseUi(uid, GpsUiKey.Key, actor);
        }
    }

    private bool IsCarriedBy(EntityUid uid, EntityUid user)
    {
        if (Transform(uid).ParentUid != user)
            return false;

        if (_hands.IsHolding(user, uid))
            return true;

        return _inventory.TryGetContainingSlot(uid, out var slot) && (slot.SlotFlags & SlotFlags.POCKET) != 0;
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var query = EntityQueryEnumerator<GpsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Sos || !comp.SosOnDeath || HasComp<EmpDisabledComponent>(uid))
                continue;

            if (FindCarrier(uid) == args.Target)
                SetSos((uid, comp), true);
        }
    }

    private void TryToggleSos(Entity<GpsComponent> ent, EntityUid user)
    {
        if (HasComp<EmpDisabledComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("adt-gps-popup-broken"), ent.Owner, user);
            return;
        }

        if (!SosReady(ent))
        {
            var left = (int) Math.Ceiling((ent.Comp.NextSosToggle - _timing.CurTime).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("adt-gps-popup-sos-cooldown", ("seconds", left)), ent.Owner, user);
            return;
        }

        SetSos(ent, !ent.Comp.Sos);

        var message = ent.Comp.Sos
            ? "adt-gps-popup-sos-enabled"
            : "adt-gps-popup-sos-disabled";

        _popup.PopupEntity(Loc.GetString(message), ent.Owner, user, PopupType.LargeCaution);
    }

    protected override void AlertSos(Entity<GpsComponent> ent)
    {
        if (ent.Comp.SosSound is not { } sound)
            return;

        var filter = Filter.Empty();

        var query = EntityQueryEnumerator<GpsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Tracking || HasComp<EmpDisabledComponent>(uid))
                continue;

            if (FindListener(uid) is { } session)
                filter.AddPlayer(session);
        }

        _audio.PlayGlobal(sound, filter, true);
    }

    protected override void UpdateUiState(Entity<GpsComponent> ent)
    {
        if (!_ui.HasUi(ent, GpsUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, GpsUiKey.Key, BuildUiState(ent));
    }

    private GpsBoundUserInterfaceState BuildUiState(Entity<GpsComponent> ent)
    {
        var comp = ent.Comp;
        var sosReadyAt = SosReady(ent) ? null : (TimeSpan?) comp.NextSosToggle;

        if (HasComp<EmpDisabledComponent>(ent))
        {
            return new GpsBoundUserInterfaceState(
                emped: true,
                tracking: comp.Tracking,
                tag: comp.Tag,
                sameMapOnly: comp.SameMapOnly,
                canToggle: comp.CanToggle,
                sos: comp.Sos,
                sosReadyAt: sosReadyAt,
                position: null,
                location: null,
                nullspace: false,
                signals: new List<GpsSignalData>());
        }

        if (!comp.Tracking)
        {
            return new GpsBoundUserInterfaceState(
                emped: false,
                tracking: false,
                tag: comp.Tag,
                sameMapOnly: comp.SameMapOnly,
                canToggle: comp.CanToggle,
                sos: comp.Sos,
                sosReadyAt: sosReadyAt,
                position: null,
                location: null,
                nullspace: false,
                signals: new List<GpsSignalData>());
        }

        var origin = _transform.GetMapCoordinates(ent.Owner);

        if (origin.MapId == MapId.Nullspace)
        {
            return new GpsBoundUserInterfaceState(
                emped: false,
                tracking: true,
                tag: comp.Tag,
                sameMapOnly: comp.SameMapOnly,
                canToggle: comp.CanToggle,
                sos: comp.Sos,
                sosReadyAt: sosReadyAt,
                position: null,
                location: null,
                nullspace: true,
                signals: new List<GpsSignalData>());
        }

        return new GpsBoundUserInterfaceState(
            emped: false,
            tracking: true,
            tag: comp.Tag,
            sameMapOnly: comp.SameMapOnly,
            canToggle: comp.CanToggle,
            sos: comp.Sos,
            sosReadyAt: sosReadyAt,
            position: ToTile(origin.Position),
            location: GetLocationName(ent.Owner),
            nullspace: false,
            signals: GetSignals(ent, origin));
    }

    private List<GpsSignalData> GetSignals(Entity<GpsComponent> reader, MapCoordinates origin)
    {
        var signals = new List<GpsSignalData>();

        var signalQuery = EntityQueryEnumerator<GpsSignalComponent>();
        while (signalQuery.MoveNext(out var uid, out var signal))
        {
            if (!signal.Enabled || uid == reader.Owner)
                continue;

            var tag = Localize(signal.Tag);
            var description = signal.Description == null ? null : Localize(signal.Description);

            if (TryBuildSignal(uid, tag, description, signal.Color, signal.SameMapOnly, false, reader, origin, out var data))
                signals.Add(data);
        }

        var deviceQuery = EntityQueryEnumerator<GpsComponent>();
        while (deviceQuery.MoveNext(out var uid, out var device))
        {
            if (!device.Tracking || uid == reader.Owner)
                continue;

            if (TryBuildSignal(uid, device.Tag, null, Color.White, false, device.Sos, reader, origin, out var data))
                signals.Add(data);
        }

        signals.Sort(CompareSignals);

        return signals;
    }

    private static int CompareSignals(GpsSignalData first, GpsSignalData second)
    {
        if (first.Sos != second.Sos)
            return first.Sos ? -1 : 1;

        var byTag = string.Compare(first.Tag, second.Tag, StringComparison.CurrentCulture);

        return byTag != 0
            ? byTag
            : first.Source.CompareTo(second.Source);
    }

    private bool TryBuildSignal(
        EntityUid source,
        string tag,
        string? description,
        Color color,
        bool sameMapOnly,
        bool sos,
        Entity<GpsComponent> reader,
        MapCoordinates origin,
        out GpsSignalData data)
    {
        data = default!;

        var coordinates = _transform.GetMapCoordinates(source);

        if (coordinates.MapId == MapId.Nullspace)
            return false;

        var sameMap = coordinates.MapId == origin.MapId;

        if (!sameMap && (sameMapOnly || reader.Comp.SameMapOnly))
            return false;

        var position = HasComp<EmpDisabledComponent>(source)
            ? null
            : (Vector2i?) ToTile(coordinates.Position);

        data = new GpsSignalData(GetNetEntity(source), tag, description, color, position, sameMap, sos);
        return true;
    }

    private string Localize(string text)
        => Loc.TryGetString(text, out var localized) ? localized : text;

    private string? GetLocationName(EntityUid uid)
    {
        var xform = Transform(uid);

        if (xform.GridUid != null)
            return Name(xform.GridUid.Value);

        if (xform.MapUid != null)
            return Name(xform.MapUid.Value);

        return null;
    }

    private ICommonSession? FindListener(EntityUid device)
    {
        return FindCarrier(device) is { } carrier && TryComp<ActorComponent>(carrier, out var actor)
            ? actor.PlayerSession
            : null;
    }

    private EntityUid? FindCarrier(EntityUid device)
    {
        var current = device;

        for (var depth = 0; depth < CarrierSearchDepth; depth++)
        {
            var parent = Transform(current).ParentUid;

            if (!parent.IsValid())
                return null;

            if (HasComp<MobStateComponent>(parent))
                return parent;

            current = parent;
        }

        return null;
    }

    private static Vector2i ToTile(Vector2 position)
    {
        return new Vector2i((int) MathF.Floor(position.X), (int) MathF.Floor(position.Y));
    }
}
