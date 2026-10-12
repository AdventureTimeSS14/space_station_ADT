using System.Numerics;
using Content.Shared.ADT.Weather.Components; // ADT-Tweak
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private readonly Dictionary<EntityUid, Vector2> _weatherOffsets = new(); // ADT-Tweak

    private void DrawWeather(
        in OverlayDrawArgs args,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers)
    {
        var worldHandle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        var stencil = _gridStencil.GetTileStencil(args,
            "weather-blocked",
            "weather-blocked-grid-stencil",
            (grid, tile) =>
            {
                _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);
                // Ignored tiles for stencil.
                return !_weather.CanWeatherAffect((grid.Owner, grid.Comp, roofComp), tile);
            });

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
            DrawWeatherGround(args, stencil, weathers, weatherOffsets, curTime);
        // ADT-Tweak-End

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(stencil.Texture, worldBounds);

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
        IRenderTexture blockedStencil,
        HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>> weathers,
        Dictionary<EntityUid, Vector2> weatherOffsets,
        TimeSpan curTime)
    {
        var worldHandle = args.WorldHandle;
        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;

        var groundStencil = _gridStencil.GetTileStencil(args,
            "weather-ground",
            "weather-ground-grid-stencil",
            (grid, tile) =>
            {
                if (_turf.IsSpace(tile))
                    return false;

                _entManager.TryGetComponent(grid.Owner, out RoofComponent? roofComp);
                return _weather.CanWeatherAffect((grid.Owner, grid.Comp, roofComp), tile);
            });

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index(StencilMask).Instance());
        worldHandle.DrawTextureRect(groundStencil.Texture, worldBounds);
        worldHandle.UseShader(_protoManager.Index(StencilUnmask).Instance());
        worldHandle.DrawTextureRect(blockedStencil.Texture, worldBounds);

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
