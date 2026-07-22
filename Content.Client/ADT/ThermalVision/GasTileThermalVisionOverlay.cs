using Content.Client.ADT.VisionOverlay;
using Content.Client.Atmos.EntitySystems;
using Content.Client.Graphics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.ADT.ThermalVision;

/// <summary>
/// Paints gas temperature as luminance before the thermal screen LUT.
/// Hot/dark fire tiles become bright so the LUT shows yellow/red instead of blue
/// (blue was just "no PointLight", not actual cold).
/// </summary>
public sealed class GasTileThermalVisionOverlay : Overlay
{
    public override bool RequestScreenTexture { get; set; } = false;

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private GasTileOverlaySystem? _gasTileOverlay;
    private readonly SharedTransformSystem _xformSys;
    private EntityQuery<GasTileOverlayComponent> _overlayQuery;

    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private List<Entity<MapGridComponent>> _grids = new();

    private readonly Color[] _colorCache = new Color[256];

    // WorldSpace = after lighting; ZIndex before ThermalVision so LUT sees this heat.
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public GasTileThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = (int)OverlayZIndexes.ThermalVisionGas;
        _xformSys = _entManager.System<SharedTransformSystem>();
        _overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

        for (byte i = 0; i <= ThermalByte.TempResolution; i++)
        {
            _colorCache[i] = ColorFromTemperatureByte(i);
        }

        // Vacuum / walls: no heat contribution
        _colorCache[ThermalByte.StateVacuum] = Color.Transparent;
        _colorCache[ThermalByte.AtmosImpossible] = Color.Transparent;
        _colorCache[ThermalByte.ReservedFuture0] = Color.Transparent;
        _colorCache[ThermalByte.ReservedFuture1] = Color.Transparent;
        _colorCache[ThermalByte.ReservedFuture2] = Color.Transparent;
    }

    /// <summary>
    /// Hotter gas → brighter/more opaque white for the thermal LUT (→ yellow/red).
    /// Full brightness at 1000K; room temp stays nearly transparent.
    /// </summary>
    private static Color ColorFromTemperatureByte(byte byteTemp)
    {
        var tempK = byteTemp * ThermalByte.TempDegreeResolution;
        // Ignore cool air; ramp from ~40°C to full white at 1000K.
        // 313K → 0, 1000K → 1.
        var t = Math.Clamp((tempK - 313f) / (1000f - 313f), 0f, 1f);
        if (t <= 0f)
            return Color.Transparent;

        var brightness = MathF.Pow(t, 0.45f);
        // Near-opaque when hot so PointLight darkness can't leave tiles looking "cold".
        var alpha = 0.35f + 0.65f * brightness;
        return new Color(brightness, brightness, brightness, alpha);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        _gasTileOverlay ??= _entManager.System<GasTileOverlaySystem>();
        if (_gasTileOverlay == null)
            return false;

        var target = args.Viewport.RenderTarget;

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());
        if (res.TemperatureTarget is null || res.TemperatureTarget.Texture.Size != target.Size)
        {
            res.TemperatureTarget?.Dispose();
            res.TemperatureTarget = _clyde.CreateRenderTarget(
                target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: nameof(GasTileThermalVisionOverlay));
        }

        var drawHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;
        var worldAABB = args.WorldAABB;
        var mapId = args.MapId;
        var worldToViewportLocal = args.Viewport.GetWorldToLocalMatrix();

        drawHandle.RenderInRenderTarget(res.TemperatureTarget,
            () =>
            {
                _grids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref _grids);

                foreach (var grid in _grids)
                {
                    if (!_overlayQuery.TryGetComponent(grid.Owner, out var comp))
                        continue;

                    var gridTileSizeVec = grid.Comp.TileSizeVector;
                    var gridTileCenterVec = grid.Comp.TileSizeHalfVector;
                    var gridEntToWorld = _xformSys.GetWorldMatrix(grid.Owner);
                    var gridEntToViewportLocal = gridEntToWorld * worldToViewportLocal;

                    drawHandle.SetTransform(gridEntToViewportLocal);

                    var worldToGridLocal = _xformSys.GetInvWorldMatrix(grid.Owner);
                    var floatBounds = worldToGridLocal.TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);

                    var localBounds = new Box2i(
                        (int)MathF.Floor(floatBounds.Left),
                        (int)MathF.Floor(floatBounds.Bottom),
                        (int)MathF.Ceiling(floatBounds.Right),
                        (int)MathF.Ceiling(floatBounds.Top));

                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);
                        while (enumerator.MoveNext(out var tileGas))
                        {
                            var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(tilePosition))
                                continue;

                            var gasColor = _colorCache[tileGas.ByteGasTemperature.Value];

                            if (gasColor.A <= 0f)
                                continue;

                            drawHandle.DrawRect(
                                Box2.CenteredAround(tilePosition + gridTileCenterVec, gridTileSizeVec),
                                gasColor
                            );
                        }
                    }
                }
            },
            new Color(0, 0, 0, 0));

        drawHandle.SetTransform(Matrix3x2.Identity);

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.TemperatureTarget != null)
            args.WorldHandle.DrawTextureRect(res.TemperatureTarget.Texture, args.WorldBounds);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? TemperatureTarget;

        public void Dispose()
        {
            TemperatureTarget?.Dispose();
        }
    }
}
