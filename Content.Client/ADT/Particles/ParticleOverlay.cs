using System.Numerics;
using Content.Shared.ADT.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Particles;

/// <summary>Draws all live particles for every active emitter each frame.</summary>
public sealed class ParticleOverlay : Overlay
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _transform;

    private readonly ParticleSystem _system;

    // Shader cache
    private readonly Dictionary<string, ShaderInstance?> _shaderCache = new();
    private readonly Dictionary<EntityUid, Matrix3x2> _coordinateMatrices = new();

    private readonly List<ActiveEmitter> _sortBuffer = new();
    private static readonly Comparison<ActiveEmitter> RenderLayerComparison =
        (a, b) => (a.Overrides?.RenderLayer ?? a.Proto.RenderLayer)
            .CompareTo(b.Overrides?.RenderLayer ?? b.Proto.RenderLayer);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ParticleOverlay(ParticleSystem system, SharedTransformSystem transform)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
        _transform = transform;
    }

    public void ClearShaderCache()
    {
        foreach (var shader in _shaderCache.Values)
            shader?.Dispose();
        _shaderCache.Clear();
    }

    protected override void DisposeBehavior()
    {
        ClearShaderCache();
        base.DisposeBehavior();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var mapId = args.MapId;
        var eyeAngle = (float)_eye.CurrentEye.Rotation;
        var cosR = MathF.Cos(-eyeAngle);
        var sinR = MathF.Sin(-eyeAngle);
        _coordinateMatrices.Clear();

        // Sort emitters, lowest layers render first
        _sortBuffer.Clear();
        int? firstRenderLayer = null;
        var requiresSort = false;
        foreach (var emitter in _system.GetEmitters())
        {
            if (emitter.MapCoords.MapId != mapId)
                continue;
            if (emitter.Frames.Length == 0)
                continue;

            _sortBuffer.Add(emitter);
            var renderLayer = emitter.Overrides?.RenderLayer ?? emitter.Proto.RenderLayer;
            if (firstRenderLayer is { } first && first != renderLayer)
                requiresSort = true;
            else
                firstRenderLayer = renderLayer;
        }

        if (_sortBuffer.Count == 0)
            return;

        if (requiresSort)
            _sortBuffer.Sort(RenderLayerComparison);

        string? activeShader = null; // track to avoid redundant calls

        foreach (var emitter in _sortBuffer)
        {
            var proto = emitter.Proto;
            var ovr = emitter.Overrides;
            var tex = emitter.Frames[emitter.AnimFrame];
            var baseHalfSize = (ovr?.ParticleSize ?? proto.ParticleSize) * 0.5f;

            // Resolve shader override takes precedence, then prototype, then null
            string? wantedShader = ovr?.Shader ?? (string.IsNullOrEmpty(proto.Shader) ? null : proto.Shader);

            if (wantedShader != activeShader)
            {
                if (wantedShader != null)
                {
                    if (!_shaderCache.TryGetValue(wantedShader, out var cached))
                    {
                        cached = _proto.TryIndex<ShaderPrototype>(wantedShader, out var shaderProto)
                            ? shaderProto.Instance()
                            : null;
                        _shaderCache[wantedShader] = cached;
                    }
                    handle.UseShader(cached);
                }
                else
                {
                    handle.UseShader(null);
                }
                activeShader = wantedShader;
            }

            var screenOrigin = emitter.MapCoords.Position;

            for (var i = 0; i < emitter.LiveTail; i++)
            {
                var particle = emitter.Particles[i];

                // Color: use ColorOverLifetime gradient if available, otherwise lerp StartColor to EndColor
                Color color;
                if (emitter.Curves.Colors is { } colorCurve)
                    color = ParticleCurveCache.Sample(colorCurve, particle.CurveIndex, particle.CurveLerp);
                else
                {
                    var t = particle.AgeRatio;
                    var startColor = ovr?.StartColor ?? proto.StartColor;
                    var endColor   = ovr?.EndColor   ?? proto.EndColor;
                    color = Color.InterpolateBetween(startColor, endColor, t);
                }

                // ColorOverride tint
                var tintColor = ovr?.ColorOverride ?? emitter.ColorOverride;
                if (tintColor is { } tint)
                    color = new Color(color.R * tint.R, color.G * tint.G, color.B * tint.B, color.A * tint.A);

                // AlphaOverLifetime: multiplied on top of color alpha
                if (emitter.Curves.Alpha is { } alphaCurve)
                {
                    var alpha = ParticleCurveCache.Sample(alphaCurve, particle.CurveIndex, particle.CurveLerp);
                    color = color.WithAlpha(color.A * alpha);
                }

                // Size: base * intensity * SizeMultiplier * SizeOverLifetime curve
                var halfSize = baseHalfSize * particle.SpawnIntensity * particle.SizeMultiplier;
                if (emitter.Curves.Size is { } sizeCurve)
                    halfSize *= ParticleCurveCache.Sample(sizeCurve, particle.CurveIndex, particle.CurveLerp);

                // Convert screen-space LocalOffset to world offset
                var local = particle.LocalOffset;
                var worldOffset = new Vector2(local.X * cosR - local.Y * sinR,
                                              local.X * sinR + local.Y * cosR);

                Vector2 worldPos;
                switch (proto.ResolvedSimulationSpace)
                {
                    case ParticleSimulationSpace.Map:
                        worldPos = particle.SpawnMapPosition + worldOffset;
                        break;
                    case ParticleSimulationSpace.Grid:
                    {
                        var coordinates = particle.SpawnCoordinates;
                        if (!_entityManager.EntityExists(coordinates.EntityId))
                            continue;
                        if (!_coordinateMatrices.TryGetValue(coordinates.EntityId, out var matrix))
                        {
                            matrix = _transform.GetWorldMatrix(coordinates.EntityId);
                            _coordinateMatrices.Add(coordinates.EntityId, matrix);
                        }

                        worldPos = Vector2.Transform(coordinates.Position + worldOffset, matrix);
                        break;
                    }
                    default:
                        worldPos = screenOrigin + worldOffset;
                        break;
                }

                // Cull particles individually. The emitter itself may already be off-screen while
                // a long-lived trail is still visible behind it.
                if (!args.WorldBounds.Contains(worldPos))
                    continue;

                // StretchFactor: elongate along velocity direction proportional to speed.
                // Rotation is derived from the velocity unit vector +precomputed eye cos/sin
                var stretchFactor = ovr?.StretchFactor ?? proto.StretchFactor;
                if (stretchFactor > 0f)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var velLen = MathF.Sqrt(velLenSq);
                        var stretchY = 1f + velLen * stretchFactor;
                        // Rotate velocity unit vector by -eyeAngle using precomputed cosR/sinR.
                        // ux = vel.X/velLen,  uy = vel.Y/velLen
                        // cV = cos(-eye+velAngle) = cosR*uy - sinR*ux
                        // sV = sin(-eye+velAngle) = sinR*uy + cosR*ux
                        var invLen = 1f / velLen;
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cV = cosR * uy - sinR * ux;
                        var sV = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cV, sV, -sV, cV, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex,
                            new Box2(-halfSize, -halfSize * stretchY, halfSize, halfSize * stretchY),
                            color);
                        continue;
                    }
                }

                // AlignToVelocity: rotate sprite to face its velocity direction.
                if (proto.AlignToVelocity)
                {
                    var velLenSq = particle.Velocity.LengthSquared();
                    if (velLenSq > 0.001f * 0.001f)
                    {
                        var invLen = 1f / MathF.Sqrt(velLenSq);
                        var ux = particle.Velocity.X * invLen;
                        var uy = particle.Velocity.Y * invLen;
                        var cos = cosR * uy - sinR * ux;
                        var sin = sinR * uy + cosR * ux;
                        handle.SetTransform(new Matrix3x2(cos, sin, -sin, cos, worldPos.X, worldPos.Y));
                        handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize, halfSize, halfSize), color);
                        continue;
                    }
                }

                // Most particles do not rotate. Reuse the eye rotation calculated once above
                // instead of doing two trig calls per particle. The boring case should be cheap. =^..^=
                if (particle.Rotation == 0f && particle.RotationSpeed == 0f)
                {
                    handle.SetTransform(new Matrix3x2(cosR, sinR, -sinR, cosR, worldPos.X, worldPos.Y));
                    handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize, halfSize, halfSize), color);
                    continue;
                }

                // Draw with rotation applied. Rotation is in radians, positive is clockwise, and 0 means "facing up" (aligned with SCREEN/eye/whatever Y axis).
                var totalRotation = -eyeAngle + particle.Rotation;
                var cosP = MathF.Cos(totalRotation);
                var sinP = MathF.Sin(totalRotation);
                handle.SetTransform(new Matrix3x2(cosP, sinP, -sinP, cosP, worldPos.X, worldPos.Y));
                handle.DrawTextureRect(tex, new Box2(-halfSize, -halfSize, halfSize, halfSize), color);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }
}
