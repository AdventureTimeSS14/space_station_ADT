using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Parallax;
using Content.Client.Weather;
using Content.Shared.ADT.Weather; // ADT-Tweak
using Content.Shared.Maps; // ADT-Tweak
using Content.Shared.Salvage;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

/// <summary>
/// Simple re-useable overlay with stencilled texture.
/// </summary>
public sealed partial class StencilOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleShader = "WorldGradientCircle";
    private static readonly ProtoId<ShaderPrototype> StencilMask = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDraw = "StencilDraw";
    private static readonly ProtoId<ShaderPrototype> StencilClear = "StencilClear"; // ADT-Tweak
    private static readonly ProtoId<ShaderPrototype> StencilEqualDraw = "StencilEqualDraw"; // ADT-Tweak

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly ParallaxSystem _parallax;
    private readonly SharedTransformSystem _transform;
    private readonly SharedMapSystem _map;
    private readonly SpriteSystem _sprite;
    private readonly WeatherSystem _weather;
    private readonly StatusEffectsSystem _statusEffects;
    private readonly TurfSystem _turf; // ADT-Tweak
    private readonly ADTWindController _wind; // ADT-Tweak
    private HashSet<Entity<WeatherStatusEffectComponent, StatusEffectComponent>>? _weatherSet = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly OverlayResourceCache<CachedResources> _resources = new();

    private readonly ShaderInstance _shader;

    public StencilOverlay(ParallaxSystem parallax, SharedTransformSystem transform, SharedMapSystem map, SpriteSystem sprite, WeatherSystem weather, StatusEffectsSystem statusEffects)
    {
        ZIndex = ParallaxSystem.ParallaxZIndex + 1;
        _parallax = parallax;
        _transform = transform;
        _map = map;
        _sprite = sprite;
        _weather = weather;
        _statusEffects = statusEffects;
        IoCManager.InjectDependencies(this);
        _turf = _entManager.System<TurfSystem>(); // ADT-Tweak
        _wind = _entManager.System<ADTWindController>(); // ADT-Tweak
        _shader = _protoManager.Index(CircleShader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mapUid = _map.GetMapOrInvalid(args.MapId);
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.Blep?.Texture.Size != args.Viewport.Size)
        {
            res.Blep?.Dispose();
            res.Blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-stencil");
        }

        // ADT-Tweak-Start
        if (res.GroundBlep?.Texture.Size != args.Viewport.Size)
        {
            res.GroundBlep?.Dispose();
            res.GroundBlep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "weather-ground-stencil");
        }
        // ADT-Tweak-End

        if (_statusEffects.TryEffectsWithComp(mapUid, out _weatherSet))
            DrawWeather(args, res, _weatherSet, invMatrix);

        if (_entManager.TryGetComponent<RestrictedRangeComponent>(mapUid, out var restrictedRangeComponent))
            DrawRestrictedRange(args, res, restrictedRangeComponent, invMatrix);

        args.WorldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? Blep;
        public IRenderTexture? GroundBlep; // ADT-Tweak

        public void Dispose()
        {
            Blep?.Dispose();
            GroundBlep?.Dispose(); // ADT-Tweak
        }
    }
}
