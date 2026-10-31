using System.Collections.Generic;
using Content.Shared.ADT.Shields;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shields;

/// <summary>
///     Портативный рассеиватель на батарее: разгоняет щит в радиусе и подавляет его восстановление.
///     Пока устройство стоит на месте, апдейт только тратит батарею, а не обновляет тайлы.
/// </summary>
public sealed partial class HandheldShieldDiffuserSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ShieldGridSystem _grid = default!;
    [Dependency] private readonly ShieldSegmentSystem _segment = default!;
    [Dependency] private readonly ShieldDiffusionSystem _diffusion = default!;

    private const string ClickSound = "/Audio/Machines/button.ogg";
    private const string BuzzSound = "/Audio/Machines/buzz-two.ogg";
    private const string SparklesProto = "ADTShieldSparkles";
    private const double ProcessInterval = 1.0;

    public override void Initialize()
    {
        SubscribeLocalEvent<HandheldShieldDiffuserComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HandheldShieldDiffuserComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HandheldShieldDiffuserComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HandheldShieldDiffuserComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<HandheldShieldDiffuserComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (_timing.CurTime < comp.NextProcess)
                continue;

            comp.NextProcess = _timing.CurTime + TimeSpan.FromSeconds(ProcessInterval);

            if (!_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery))
            {
                SetEnabled(uid, comp, false);
                continue;
            }

            if (!_battery.TryUseCharge((battery.Value.Owner, battery.Value.Comp), comp.ActivePowerUse))
            {
                SetEnabled(uid, comp, false);
                continue;
            }

            RefreshIfMoved(uid, comp);
        }
    }

    private void OnMapInit(EntityUid uid, HandheldShieldDiffuserComponent comp, MapInitEvent args)
        => UpdateVisuals(uid, comp);

    private void OnUseInHand(EntityUid uid, HandheldShieldDiffuserComponent comp, UseInHandEvent args)
    {
        if (comp.Enabled)
        {
            SetEnabled(uid, comp, false);
            _audio.PlayPvs(ClickSound, Transform(uid).Coordinates);
            args.Handled = true;
            return;
        }

        if (!_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery))
        {
            _audio.PlayPvs(BuzzSound, Transform(uid).Coordinates);
            args.Handled = true;
            return;
        }

        SetEnabled(uid, comp, true);
        _audio.PlayPvs(ClickSound, Transform(uid).Coordinates);
        SuppressAround(uid, comp, disperse: true);
        args.Handled = true;
    }

    private void OnExamined(EntityUid uid, HandheldShieldDiffuserComponent comp, ExaminedEvent args)
    {
        var enabled = Loc.GetString(comp.Enabled ? "shield-handheld-enabled" : "shield-handheld-disabled");
        args.PushMarkup(enabled);

        if (_powerCell.TryGetBatteryFromEntityOrSlot(uid, out var battery))
        {
            var charge = _battery.GetCharge((battery.Value.Owner, battery.Value.Comp));
            var uses = charge / MathF.Max(1, comp.ActivePowerUse);
            args.PushMarkup(Loc.GetString("shield-handheld-uses-left", ("amount", (int) uses)));
        }
    }

    private void OnShutdown(EntityUid uid, HandheldShieldDiffuserComponent comp, ComponentShutdown args)
    {
        UnsuppressAll(comp);
    }

    private void RefreshIfMoved(EntityUid uid, HandheldShieldDiffuserComponent comp)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            UnsuppressAll(comp);
            comp.HasLastPosition = false;
            return;
        }

        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (comp.HasLastPosition && comp.LastGrid == gridUid && comp.LastTile == center)
            return;

        SuppressAround(uid, comp, gridUid, center, disperse: false);
    }

    private void SuppressAround(EntityUid uid, HandheldShieldDiffuserComponent comp, bool disperse)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            UnsuppressAll(comp);
            comp.HasLastPosition = false;
            return;
        }

        SuppressAround(uid, comp, gridUid, _map.TileIndicesFor(gridUid, grid, xform.Coordinates), disperse);
    }

    private void SuppressAround(EntityUid uid, HandheldShieldDiffuserComponent comp, EntityUid gridUid, Vector2i center, bool disperse)
    {
        UnsuppressAll(comp);
        comp.LastGrid = gridUid;
        comp.LastTile = center;
        comp.HasLastPosition = true;

        var radius = comp.ActiveRadius;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var tile = center + new Vector2i(x, y);
                comp.SuppressedTiles.Add((gridUid, tile));
                _diffusion.Add(gridUid, tile, comp.DiffuseDuration);
                DiffuseSegment(comp, gridUid, tile, disperse);
            }
        }
    }

    private void DiffuseSegment(HandheldShieldDiffuserComponent comp, EntityUid gridUid, Vector2i tile, bool disperse)
    {
        var seg = _grid.GetSegmentAt(gridUid, tile);
        if (!Exists(seg) || !TryComp<ShieldSegmentComponent>(seg, out var segComp))
            return;

        var alreadyDown = segComp.Generator is { } genUid
            && TryComp<ShieldGeneratorComponent>(genUid, out var gen)
            && gen.Tiles.TryGetValue(tile, out var data)
            && data.DiffusedFor > 0;

        if (disperse || !alreadyDown)
        {
            var coords = Transform(seg).Coordinates;
            Spawn(SparklesProto, coords);
        }

        _segment.Diffuse(seg, segComp, comp.DiffuseDuration);
    }

    private void UnsuppressAll(HandheldShieldDiffuserComponent comp)
    {
        foreach (var (grid, tile) in comp.SuppressedTiles)
            _diffusion.Remove(grid, tile);
        comp.SuppressedTiles.Clear();
    }

    private void SetEnabled(EntityUid uid, HandheldShieldDiffuserComponent comp, bool enabled)
    {
        comp.Enabled = enabled;
        if (!enabled)
        {
            UnsuppressAll(comp);
            comp.HasLastPosition = false;
        }
        UpdateVisuals(uid, comp);
    }

    private void UpdateVisuals(EntityUid uid, HandheldShieldDiffuserComponent comp)
    {
        if (!TryComp<AppearanceComponent>(uid, out _))
            return;

        _appearance.SetData(uid, HandheldShieldDiffuserVisuals.Enabled, comp.Enabled);
    }
}