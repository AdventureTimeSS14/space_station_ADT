using System.Numerics;
using Content.Shared.ADT.Weather.Components; // ADT-Tweak
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Graphics;
using Robust.Shared.Map.Components;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private List<Entity<MapGridComponent>> _grids = new();
    private readonly Dictionary<EntityUid, Vector2> _weatherOffsets = new(); // ADT-Tweak

    private void DrawWeather(
        in OverlayDrawArgs args,
        CachedResources res,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers,
        Matrix3x2 invMatrix)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(res.Blep!,
            () =>
            {
                var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
                _grids.Clear();

                // idk if this is safe to cache in a field and clear sloth help
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref _grids);

                foreach (var grid in _grids)
                {
                    var matrix = _transform.GetWorldMatrix(grid, xformQuery);
                    var matty = Matrix3x2.Multiply(matrix, invMatrix);
                    worldHandle.SetTransform(matty);
                    _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);

                    foreach (var tile in _map.GetTilesIntersecting(grid.Owner, grid, worldAABB))
                    {
                        // Ignored tiles for stencil
                        if (_weather.CanWeatherAffect((grid.Owner, grid, roofComp), tile))
                            continue;

                        var gridTile = new Box2(tile.GridIndices * grid.Comp.TileSize,
                            (tile.GridIndices + Vector2i.One) * grid.Comp.TileSize);

                        worldHandle.DrawRect(gridTile, Color.White);
                    }
                }
            },
            Color.Transparent);

        // ADT-Tweak-Start
        var curTime = _timing.RealTime;
        var weatherOffsets = _weatherOffsets;
        weatherOffsets.Clear();
        var hasGroundLayer = false;

        foreach (var (uid, weather, _) in weathers)
        {
            var hash = (uint) uid.GetHashCode();
            weatherOffsets[uid] = new Vector2(hash % 2000 - 1000f, hash / 2000 % 2000 - 1000f);

            if (weather.GroundSprite != null)
                hasGroundLayer = true;
        }

        if (hasGroundLayer)
            DrawWeatherGround(args, res, weathers, invMatrix, weatherOffsets, curTime);
        // ADT-Tweak-End

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(res.Blep!.Texture, worldBounds);

        foreach (var (uid, weather, status) in weathers)
        {
            var alpha = _weather.GetWeatherPercent((uid, status));
            var sprite = _sprite.GetFrame(weather.Sprite, curTime);

            // Draw the rain
            worldHandle.UseShader(_protoManager.Index(StencilDraw).Instance());
            // ADT-Tweak-Start
            DrawWeatherLayer(worldHandle,
                worldAABB,
                sprite,
                curTime,
                position,
                weather.Scrolling ?? Vector2.Zero,
                weatherOffsets[uid],
                GetWeatherRotation(uid),
                (weather.Color ?? Color.White).WithAlpha(alpha));
            // ADT-Tweak-End
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }

    // ADT-Tweak-Start
    private void DrawWeatherGround(
        in OverlayDrawArgs args,
        CachedResources res,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers,
        Matrix3x2 invMatrix,
        Dictionary<EntityUid, Vector2> weatherOffsets,
        TimeSpan curTime)
    {
        var worldHandle = args.WorldHandle;
        var mapId = args.MapId;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        worldHandle.RenderInRenderTarget(res.GroundBlep!,
            () =>
            {
                var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
                _grids.Clear();
                _mapManager.FindGridsIntersecting(mapId, worldAABB, ref _grids);

                foreach (var grid in _grids)
                {
                    var matrix = _transform.GetWorldMatrix(grid, xformQuery);
                    var matty = Matrix3x2.Multiply(matrix, invMatrix);
                    worldHandle.SetTransform(matty);
                    _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);

                    foreach (var tile in _map.GetTilesIntersecting(grid.Owner, grid, worldAABB))
                    {
                        if (_turf.IsSpace(tile))
                            continue;

                        if (!_weather.CanWeatherAffect((grid.Owner, grid, roofComp), tile))
                            continue;

                        var gridTile = new Box2(tile.GridIndices * grid.Comp.TileSize,
                            (tile.GridIndices + Vector2i.One) * grid.Comp.TileSize);

                        worldHandle.DrawRect(gridTile, Color.White);
                    }
                }
            },
            Color.Transparent);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(res.GroundBlep!.Texture, worldBounds);

        foreach (var (uid, weather, status) in weathers)
        {
            if (weather.GroundSprite is not { } groundSprite)
                continue;

            var alpha = _weather.GetWeatherPercent((uid, status));
            var sprite = _sprite.GetFrame(groundSprite, curTime);

            worldHandle.UseShader(_protoManager.Index(StencilEqualDraw).Instance());
            DrawWeatherLayer(worldHandle,
                worldAABB,
                sprite,
                curTime,
                position,
                Vector2.Zero,
                weatherOffsets[uid],
                GetWeatherRotation(uid),
                (weather.Color ?? Color.White).WithAlpha(alpha));
        }

        worldHandle.UseShader(_protoManager.Index(StencilClear).Instance());
        worldHandle.DrawRect(worldBounds, Color.White);
    }

    private Angle GetWeatherRotation(EntityUid weather)
    {
        if (!_entManager.TryGetComponent<ADTWeatherWindComponent>(weather, out var wind))
            return Angle.Zero;

        return _wind.GetWindDirection(wind);
    }

    private void DrawWeatherLayer(
        DrawingHandleWorld worldHandle,
        Box2 worldAABB,
        Texture sprite,
        TimeSpan curTime,
        Vector2 position,
        Vector2 scrolling,
        Vector2 offset,
        Angle rotation,
        Color color)
    {
        var center = worldAABB.Center;
        var area = new Box2Rotated(worldAABB, -rotation, center).CalcBoundingBox();
        var transform = Matrix3x2.CreateTranslation(-offset) * Matrix3x2.CreateRotation((float) rotation.Theta, center);

        worldHandle.SetTransform(transform);
        _parallax.DrawParallax(worldHandle,
            area.Translated(offset),
            sprite,
            curTime,
            position,
            scrolling,
            modulate: color);
        worldHandle.SetTransform(Matrix3x2.Identity);
    }
    // ADT-Tweak-End
}
