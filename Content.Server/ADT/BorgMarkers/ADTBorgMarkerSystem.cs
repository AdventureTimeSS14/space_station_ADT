using Content.Server.Popups;
using Content.Shared.ADT.BorgMarkers;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.ADT.BorgMarkers;

public sealed class ADTBorgMarkerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgMarkerPlacerComponent, ADTPlaceBorgMarkerEvent>(OnPlace);
        SubscribeLocalEvent<ADTBorgMarkerPlacerComponent, ADTCycleBorgMarkerColorEvent>(OnCycleColor);
        SubscribeLocalEvent<ADTBorgMarkerPlacerComponent, ADTClearBorgMarkersEvent>(OnClear);
        SubscribeLocalEvent<ADTBorgMarkerPlacerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnPlace(Entity<ADTBorgMarkerPlacerComponent> ent, ref ADTPlaceBorgMarkerEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        Prune(ent);

        if (TryFindMarkerAt(ent, args.Target, out var existing))
        {
            RemoveMarker(ent, existing);
            _popup.PopupEntity(Loc.GetString("adt-borg-marker-removed"), ent, ent);
            return;
        }

        if (ent.Comp.Markers.Count >= ent.Comp.MaxMarkers)
        {
            _popup.PopupEntity(
                Loc.GetString("adt-borg-marker-limit", ("limit", ent.Comp.MaxMarkers)),
                ent,
                ent);
            return;
        }

        var color = GetSelectedColor(ent);

        var marker = Spawn(ent.Comp.MarkerProto, args.Target);
        var markerComp = EnsureComp<ADTBorgMarkerComponent>(marker);
        markerComp.MarkerColor = color?.Color ?? Color.Cyan;
        Dirty(marker, markerComp);

        _pvs.AddGlobalOverride(marker);

        ent.Comp.Markers.Add(marker);

        _audio.PlayPvs(ent.Comp.SoundOnPlace, ent);

        _popup.PopupEntity(
            color == null
                ? Loc.GetString("adt-borg-marker-placed")
                : Loc.GetString("adt-borg-marker-placed-color", ("color", Loc.GetString(color.Name))),
            ent,
            ent);
    }

    private void OnCycleColor(Entity<ADTBorgMarkerPlacerComponent> ent, ref ADTCycleBorgMarkerColorEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Palette.Count == 0)
            return;

        ent.Comp.SelectedColor = (ent.Comp.SelectedColor + 1) % ent.Comp.Palette.Count;

        var color = ent.Comp.Palette[ent.Comp.SelectedColor];
        _popup.PopupEntity(
            Loc.GetString("adt-borg-marker-color-selected", ("color", Loc.GetString(color.Name))),
            ent,
            ent);
    }

    private void OnClear(Entity<ADTBorgMarkerPlacerComponent> ent, ref ADTClearBorgMarkersEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        Prune(ent);

        var count = ent.Comp.Markers.Count;
        ClearMarkers(ent);

        _popup.PopupEntity(Loc.GetString("adt-borg-marker-cleared", ("count", count)), ent, ent);
    }

    private void OnShutdown(Entity<ADTBorgMarkerPlacerComponent> ent, ref ComponentShutdown args)
    {
        ClearMarkers(ent);
    }

    private ADTBorgMarkerColor? GetSelectedColor(Entity<ADTBorgMarkerPlacerComponent> ent)
    {
        if (ent.Comp.Palette.Count == 0)
            return null;

        var index = Math.Clamp(ent.Comp.SelectedColor, 0, ent.Comp.Palette.Count - 1);
        return ent.Comp.Palette[index];
    }

    private bool TryFindMarkerAt(Entity<ADTBorgMarkerPlacerComponent> ent, EntityCoordinates target, out EntityUid marker)
    {
        marker = default;

        var targetMap = _transform.ToMapCoordinates(target);

        foreach (var candidate in ent.Comp.Markers)
        {
            var candidateMap = _transform.GetMapCoordinates(candidate);
            if (candidateMap.MapId != targetMap.MapId)
                continue;

            if ((candidateMap.Position - targetMap.Position).Length() > ent.Comp.RemoveRange)
                continue;

            marker = candidate;
            return true;
        }

        return false;
    }

    private void RemoveMarker(Entity<ADTBorgMarkerPlacerComponent> ent, EntityUid marker)
    {
        ent.Comp.Markers.Remove(marker);

        if (!Deleted(marker))
        {
            _pvs.RemoveGlobalOverride(marker);
            QueueDel(marker);
        }
    }

    private void ClearMarkers(Entity<ADTBorgMarkerPlacerComponent> ent)
    {
        foreach (var marker in ent.Comp.Markers)
        {
            if (Deleted(marker))
                continue;

            _pvs.RemoveGlobalOverride(marker);
            QueueDel(marker);
        }

        ent.Comp.Markers.Clear();
    }

    private void Prune(Entity<ADTBorgMarkerPlacerComponent> ent)
    {
        ent.Comp.Markers.RemoveAll(marker => Deleted(marker));
    }
}
