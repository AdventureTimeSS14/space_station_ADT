using System.Numerics;
using Content.Shared.ADT.GPS;
using Content.Shared.Emp;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.GPS;

public sealed class GpsSystem : SharedGpsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GpsComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<GpsComponent, GpsToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<GpsComponent, GpsToggleRangeMessage>(OnToggleRangeMessage);
        SubscribeLocalEvent<GpsComponent, GpsSetTagMessage>(OnSetTagMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<GpsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
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

    private void OnSetTagMessage(Entity<GpsComponent> ent, ref GpsSetTagMessage args)
    {
        SetTag(ent, args.Tag);
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

        if (HasComp<EmpDisabledComponent>(ent))
        {
            return new GpsBoundUserInterfaceState(
                emped: true,
                tracking: comp.Tracking,
                tag: comp.Tag,
                sameMapOnly: comp.SameMapOnly,
                canToggle: comp.CanToggle,
                position: null,
                location: null,
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
                position: null,
                location: null,
                signals: new List<GpsSignalData>());
        }

        var origin = _transform.GetMapCoordinates(ent.Owner);

        return new GpsBoundUserInterfaceState(
            emped: false,
            tracking: true,
            tag: comp.Tag,
            sameMapOnly: comp.SameMapOnly,
            canToggle: comp.CanToggle,
            position: ToTile(origin.Position),
            location: GetLocationName(ent.Owner),
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

            if (TryBuildSignal(uid, tag, description, signal.Color, signal.SameMapOnly, reader, origin, out var data))
                signals.Add(data);
        }

        var deviceQuery = EntityQueryEnumerator<GpsComponent>();
        while (deviceQuery.MoveNext(out var uid, out var device))
        {
            if (!device.Tracking || uid == reader.Owner)
                continue;

            if (TryBuildSignal(uid, device.Tag, null, Color.White, false, reader, origin, out var data))
                signals.Add(data);
        }

        signals.Sort((first, second) => GetSortWeight(first, origin).CompareTo(GetSortWeight(second, origin)));

        return signals;
    }

    private bool TryBuildSignal(
        EntityUid source,
        string tag,
        string? description,
        Color color,
        bool sameMapOnly,
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

        data = new GpsSignalData(tag, description, color, position, sameMap);
        return true;
    }

    private string Localize(string text)
        => Loc.TryGetString(text, out var localized) ? localized : text;

    private static float GetSortWeight(GpsSignalData signal, MapCoordinates origin)
    {
        if (!signal.SameMap || signal.Position == null)
            return float.MaxValue;

        return (signal.Position.Value - ToTile(origin.Position)).Length;
    }

    private string? GetLocationName(EntityUid uid)
    {
        var xform = Transform(uid);

        if (xform.GridUid != null)
            return Name(xform.GridUid.Value);

        if (xform.MapUid != null)
            return Name(xform.MapUid.Value);

        return null;
    }

    private static Vector2i ToTile(Vector2 position)
    {
        return new Vector2i((int) MathF.Floor(position.X), (int) MathF.Floor(position.Y));
    }
}
