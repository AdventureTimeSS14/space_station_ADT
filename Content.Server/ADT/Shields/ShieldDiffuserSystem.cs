using Content.Shared.ADT.Shields;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server.ADT.Shields;

/// <summary>Напольный рассеиватель: убирает щит прямо над собой и не даёт полю восстановиться на этой плитке.</summary>
public sealed partial class ShieldDiffuserSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ShieldGridSystem _grid = default!;
    [Dependency] private readonly ShieldSegmentSystem _segment = default!;
    [Dependency] private readonly ShieldDiffusionSystem _diffusion = default!;

    private const string ClickSound = "/Audio/Machines/button.ogg";

    public override void Initialize()
    {
        SubscribeLocalEvent<ShieldDiffuserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShieldDiffuserComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ShieldDiffuserComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShieldDiffuserComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ShieldDiffuserComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved);
    }

    private void OnMapInit(EntityUid uid, ShieldDiffuserComponent comp, MapInitEvent args)
    {
        SetSuppression(uid, comp, comp.Enabled);
        if (comp.Enabled)
            DiffuseAround(uid, comp);
        UpdateVisuals(uid, comp);
    }

    private void OnActivate(EntityUid uid, ShieldDiffuserComponent comp, ref ActivateInWorldEvent args)
    {
        _audio.PlayPvs(ClickSound, Transform(uid).Coordinates);

        if (comp.Alarm > 0)
        {
            comp.Alarm = 0;
            UpdateVisuals(uid, comp);
            args.Handled = true;
            return;
        }

        comp.Enabled = !comp.Enabled;

        SetSuppression(uid, comp, comp.Enabled);
        if (comp.Enabled)
            DiffuseAround(uid, comp);
        UpdateVisuals(uid, comp);
        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, ShieldDiffuserComponent comp, ComponentShutdown args)
    {
        SetSuppression(uid, comp, false);
    }

    private void OnEmpPulse(EntityUid uid, ShieldDiffuserComponent comp, ref EmpPulseEvent args)
    {
        comp.Alarm = MathF.Max(comp.Alarm, comp.AlarmDuration);
        args.Affected = true;
        args.Disabled = true;
        UpdateVisuals(uid, comp);
    }

    private void OnEmpDisabledRemoved(EntityUid uid, ShieldDiffuserComponent comp, ref EmpDisabledRemovedEvent args)
    {
        if (comp.Alarm <= 0)
            return;

        comp.Alarm = 0;
        UpdateVisuals(uid, comp);
    }

    private void SetSuppression(EntityUid uid, ShieldDiffuserComponent comp, bool enabled)
    {
        if (!enabled)
        {
            foreach (var (grid, tile) in comp.DiffusedTiles)
                _diffusion.Remove(grid, tile);
            comp.DiffusedTiles.Clear();
            return;
        }

        if (GetAffectedTile(uid) is not { } affected)
            return;

        _diffusion.Add(affected.Grid, affected.Tile, comp.DiffuseRefresh);
        comp.DiffusedTiles.Add((affected.Grid, affected.Tile));
    }

    private (EntityUid Grid, Vector2i Tile)? GetAffectedTile(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return null;

        return (gridUid, _map.TileIndicesFor(gridUid, grid, xform.Coordinates));
    }

    private void DiffuseAround(EntityUid uid, ShieldDiffuserComponent comp)
    {
        if (GetAffectedTile(uid) is not { } affected)
            return;

        var seg = _grid.GetSegmentAt(affected.Grid, affected.Tile);
        if (Exists(seg) && TryComp<ShieldSegmentComponent>(seg, out var segComp))
            _segment.Diffuse(seg, segComp, comp.DiffuseRefresh);
    }

    private void UpdateVisuals(EntityUid uid, ShieldDiffuserComponent comp)
    {
        if (!TryComp<AppearanceComponent>(uid, out _))
            return;

        var state = comp.Alarm > 0
            ? ShieldDiffuserState.Emergency
            : comp.Enabled
                ? ShieldDiffuserState.On
                : ShieldDiffuserState.Off;

        _appearance.SetData(uid, ShieldDiffuserVisuals.State, state);
    }
}