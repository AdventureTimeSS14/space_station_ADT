using System.Numerics;
using Content.Shared.ADT.Particles;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Particles;

public readonly struct ParticleSettingsChangedEvent
{
    public readonly int Quality;

    public ParticleSettingsChangedEvent(int quality) => Quality = quality;
}

/// <summary>
/// Manages active particle emitters on the client, including their simulation and rendering via <see cref="ParticleOverlay"/>.
/// </summary>
public sealed partial class ParticleSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private readonly List<ActiveEmitter> _emitters = new();
    private readonly Dictionary<uint, ActiveEmitter> _emittersByHandle = new();
    private readonly List<(ProtoId<ParticleEffectPrototype> Id, MapCoordinates Coords, int Depth)> _pendingSubEmitters = new();

    /// <summary>Maximum number of sub-emitter chains allowed. Prevents infinite recursive sub-emitter chains.</summary>
    public const int MaxSubEmitterDepth = 3;
    private ParticleOverlay _overlay = default!;

    // Tally of live particles across all emitters. Incremented in EmitParticle, decremented on every kill path.
    private int _liveParticleCount;

    // Per-prototype texture/delay cache so multiple emitters sharing a prototype don't re-resolve the same RSI frames.
    private readonly Dictionary<string, (Texture[] Frames, float[] Delays)> _frameCache = new();
    // Pre-baked curve lookup tables, shared by every emitter of the same prototype.
    private readonly Dictionary<string, ParticleCurveCache> _curveCache = new();
    // Prototypes that failed to resolve; skip re-attempting every emitter spawn.
    private readonly HashSet<string> _frameResolveFailures = new();

    private int _quality;
    private int _globalBudget;
    private float _smoothedFrameTime = 1f / 60f;

    private Action<int>? _onGlobalBudgetChanged;
    private Action<int>? _onAutoLodChanged;

    private const float MinLodScale = 0.25f;
    private int _lodThreshold = 48;
    private float _lodScale = 1f;
    private int _inViewEmitters;

    private const int SortIntervalFrames = 10;
    private int _sortCooldown;

    // Incrementing handle counter. Old values are abandoned when emitters die, handles are never reused.
    // uint gives ~4 billion spawns before wrapping, if this causes problems, you scare me.
    private uint _nextHandle = 1;

    // Emission/count multipliers per quality level: Off, Low, Medium, High.
    // So when quality is set to Low, only 25% of the particles spawn, at Medium it's 50%, and at High it's 100%.
    private static readonly float[] QualityMultipliers = { 0f, 0.25f, 0.5f, 1f };

    // Default global particle budgets per quality level.
    // Off still needs a nonzero budget so IgnoreQualitySettings effects can render (8 emitters * 64 particles).
    private static readonly int[] QualityBudgets = { 512, 2250, 5500, 8000 };

    /// <summary>
    /// Absolute ceiling on live particles regardless of quality settings or anything else.
    /// In isolated testing, I was able to spawn ~26,000 simultaneous particles before significant frame drops.
    /// This number is NOT a target and MUST NOT be treated as one and is intentionally set well below that for several reasons:
    /// <list type="bullet">
    ///   <item><b>All particle simulation runs entirely on the CPU</b>. Every particle competes
    ///   with gameplay logic, physics, networking, and rendering on the same thread.</item>
    ///   <item>That 26k figure was measured in isolation. In a real round with entities, atmos, and players,
    ///   performance will degrade significantly sooner.</item>
    ///   <item>Emitters stack multiplicatively. Ten "small" effects at 500 particles each is already
    ///   5,000 particles before considering anything else in the scene.</item>
    /// </list>
    /// I would KILL to be able to render these on the GPU, but that is not currently an option without engine changes.
    /// <b>Do not raise this limit just because your machine can handle it.</b>
    /// This limit exists to protect performance across all hardware and real gameplay conditions.
    /// If you believe this needs to be increased, you should first justify why the effect cannot
    /// be achieved more efficiently. You do NOT need that many particles.
    /// </summary>
    private const int HardMaxParticles = 8000;

    /// <summary>
    /// Maximum particles per emitter for <see cref="ParticleEffectPrototype.IgnoreQualitySettings"/> effects
    /// when quality is below High. At High quality they respect the full <see cref="HardMaxParticles"/> ceiling.
    /// </summary>
    private const int IgnoreQualityMaxParticles = 64;

    // A Burst emitter uses MaxCount as its requested spawn count, but will not spawn more than this number of particles in a single burst.
    private const int DefaultBurstMaxCount = 50;

    /// <summary>Maximum calculated MaxCount when a prototype does not provide one explicitly.</summary>
    private const int AutomaticMaxCount = 128;

    private const float MaxSimulationStep = 0.1f;

    private const float MaxSimulationDebt = 0.25f;

    #region =^..^= Particle System API =^..^=
    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ParticleOverlay(this, _transform);
        _overlayManager.AddOverlay(_overlay);

        _cfg.OnValueChanged(CCVars.ParticleQuality, OnQualityChanged, invokeImmediately: true);
        _cfg.OnValueChanged(CCVars.ParticleGlobalBudget, _onGlobalBudgetChanged = v =>
        {
            if (_quality == QualityBudgets.Length - 1)
                _globalBudget = v;
        });
        _cfg.OnValueChanged(CCVars.ParticleAutoLodEmitters, _onAutoLodChanged = v => _lodThreshold = v, invokeImmediately: true);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.ParticleQuality, OnQualityChanged);
        if (_onGlobalBudgetChanged != null)
            _cfg.UnsubValueChanged(CCVars.ParticleGlobalBudget, _onGlobalBudgetChanged);
        if (_onAutoLodChanged != null)
            _cfg.UnsubValueChanged(CCVars.ParticleAutoLodEmitters, _onAutoLodChanged);
        _overlayManager.RemoveOverlay(_overlay);
        _emitters.Clear();
        _emittersByHandle.Clear();
        _curveCache.Clear();
        _liveParticleCount = 0;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        // Baked frame/curve caches hold stale data after YAML hot-reloads; new emitters re-bake on spawn.
        _frameCache.Clear();
        _curveCache.Clear();
        _frameResolveFailures.Clear();
        _overlay.ClearShaderCache();
    }

    private void OnQualityChanged(int quality)
    {
        _quality = quality;
        _inViewEmitters = 0;
        if (quality >= 0 && quality < QualityBudgets.Length)
        {
            _globalBudget = quality == QualityBudgets.Length - 1
                ? _cfg.GetCVar(CCVars.ParticleGlobalBudget)
                : QualityBudgets[quality];
        }

        RaiseLocalEvent(new ParticleSettingsChangedEvent(quality));
    }

    public IReadOnlyList<ActiveEmitter> GetEmitters() => _emitters;

    /// <summary>
    /// Immediately destroys all active emitters and kills every live particle.
    /// This is the nuclear option, use it when something has gone very wrong.
    /// </summary>
    /// <returns>Number of emitters that were cleared.</returns>
    public int KillAll()
    {
        var count = _emitters.Count;
        _emitters.Clear();
        _emittersByHandle.Clear();
        _liveParticleCount = 0;
        return count;
    }

    /// <summary>Spawns a particle effect at a given map coordinate.</summary>
    public ActiveEmitter? SpawnEffect(ProtoId<ParticleEffectPrototype> effectId, MapCoordinates coords, EntityUid? attachedEntity = null, Color? colorOverride = null, ParticleRuntimeOverrides? overrides = null, Vector2? initialVelocity = null)
        => SpawnEffect(effectId, coords, depth: 0, attachedEntity: attachedEntity, colorOverride: colorOverride, overrides: overrides, initialVelocity: initialVelocity);

    private ActiveEmitter? SpawnEffect(ProtoId<ParticleEffectPrototype> effectId, MapCoordinates coords, int depth, EntityUid? attachedEntity = null, Color? colorOverride = null, ParticleRuntimeOverrides? overrides = null, Vector2? initialVelocity = null)
    {
        if (coords.MapId == MapId.Nullspace || !_mapSystem.MapExists(coords.MapId))
            return null;

        if (depth > MaxSubEmitterDepth)
        {
            Log.Warning($"ParticleSystem: subemitter depth exceeded MaxSubEmitterDepth ({MaxSubEmitterDepth}). Dropping '{effectId}'. DO NOT RECUSIVELY STACK SUBEMITTERS.");
            return null;
        }

        if (!_protoManager.TryIndex(effectId, out var proto))
            return null;

        // Skip quality check if this is a gameplay-critical particle
        if (_quality == 0 && !proto.IgnoreQualitySettings)
            return null;

        // Even IgnoreQualitySettings effects are capped at 8 simultaneous emitters when quality is Off.
        if (_quality == 0 && proto.IgnoreQualitySettings)
        {
            var ignoreQualityEmitterCount = 0;
            foreach (var e in _emitters)
            {
                if (e.Proto.IgnoreQualitySettings)
                    ignoreQualityEmitterCount++;
            }
            if (ignoreQualityEmitterCount >= 8)
                return null;
        }

        var emitter = CreateEmitter(proto, coords, attachedEntity);
        if (emitter == null)
            return null;

        emitter.ColorOverride = colorOverride;
        emitter.SubEmitterDepth = depth;

        if (overrides != null)
            ApplyOverrides(emitter, overrides);

        // Pre-seed velocity so burst emitters can use InheritVelocity correctly.
        if (initialVelocity.HasValue)
        {
            emitter.EmitterVelocity = initialVelocity.Value;
            emitter.PreviousPosition = coords.Position;
            emitter.VelocityInitialized = true;
        }

        // Add before BurstEmit so the live count is tracked correctly when EmitParticle runs.
        _emitters.Add(emitter);
        _emittersByHandle.Add(emitter.Handle, emitter);

        if (proto.Burst)
            BurstEmit(emitter);

        return emitter;
    }

    private static readonly Comparison<ActiveEmitter> FramePriorityComparison =
        (a, b) => a.FramePriority.CompareTo(b.FramePriority);

    /// <summary>
    /// Patches runtime overrides on a live emitter by handle.
    /// Only non-null fields are applied, null fields are left unchanged.
    /// </summary>
    public void UpdateRuntime(uint handle, ParticleRuntimeOverrides overrides)
    {
        if (handle == 0)
            return;
        if (_emittersByHandle.TryGetValue(handle, out var emitter))
            ApplyOverrides(emitter, overrides);
    }

    /// <summary>
    /// Patches runtime overrides on a live emitter by direct reference.
    /// Use this when you already have the <see cref="ActiveEmitter"/> from <see cref="SpawnEffect"/>.
    /// </summary>
    public static void UpdateRuntime(ActiveEmitter emitter, ParticleRuntimeOverrides overrides)
        => ApplyOverrides(emitter, overrides);

    private static void ApplyOverrides(ActiveEmitter emitter, ParticleRuntimeOverrides src)
    {
        emitter.Overrides ??= new ParticleRuntimeOverrides();
        var dst = emitter.Overrides;

        // ᓚᘏᗢ <( here lies the "UNHOLY IF STATEMENT BLOCK AND DESPAIR" you were not missed.
        // Anyway we override everything in this big block
        dst.StartColor = src.StartColor ?? dst.StartColor;
        dst.EndColor = src.EndColor ?? dst.EndColor;
        dst.ColorOverride = src.ColorOverride ?? dst.ColorOverride;
        dst.Shader = src.Shader ?? dst.Shader;
        dst.RenderLayer = src.RenderLayer ?? dst.RenderLayer;
        dst.ParticleSize = src.ParticleSize ?? dst.ParticleSize;
        dst.SizeVariance = src.SizeVariance ?? dst.SizeVariance;
        dst.StretchFactor = src.StretchFactor ?? dst.StretchFactor;
        dst.Lifetime = src.Lifetime ?? dst.Lifetime;
        dst.LifetimeVariance = src.LifetimeVariance ?? dst.LifetimeVariance;
        dst.Speed = src.Speed ?? dst.Speed;
        dst.SpeedVariance = src.SpeedVariance ?? dst.SpeedVariance;
        dst.ConstantForce = src.ConstantForce ?? dst.ConstantForce;
        dst.Gravity = src.Gravity ?? dst.Gravity;
        dst.Drag = src.Drag ?? dst.Drag;
        dst.TerminalSpeed = src.TerminalSpeed ?? dst.TerminalSpeed;
        dst.NoiseStrength = src.NoiseStrength ?? dst.NoiseStrength;
        dst.NoiseFrequency = src.NoiseFrequency ?? dst.NoiseFrequency;
        dst.InheritVelocity = src.InheritVelocity ?? dst.InheritVelocity;
        dst.StartRotation = src.StartRotation ?? dst.StartRotation;
        dst.StartRotationVariance = src.StartRotationVariance ?? dst.StartRotationVariance;
        dst.RotationSpeed = src.RotationSpeed ?? dst.RotationSpeed;
        dst.RotationSpeedVariance = src.RotationSpeedVariance ?? dst.RotationSpeedVariance;
        dst.EmissionRate = src.EmissionRate ?? dst.EmissionRate;
        dst.MaxCount = src.MaxCount ?? dst.MaxCount;
        dst.Duration = src.Duration ?? dst.Duration;
        dst.SpreadAngle = src.SpreadAngle ?? dst.SpreadAngle;
        dst.SpawnOffset = src.SpawnOffset ?? dst.SpawnOffset;

        if (src.EmitAngle is { } emitAngle)
        {
            dst.EmitAngle = emitAngle;
            if (emitter.TargetEntity == null && emitter.TargetPosition == null)
                emitter.EffectiveEmitAngle = (float)emitAngle.Theta;
        }
    }

    /// <summary>
    /// Spawns a particle effect whose emission direction tracks a target entity each tick.
    /// When the entity is deleted the emitter retains its last angle.
    /// </summary>
    public ActiveEmitter? SpawnEffectAimAt(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        EntityUid targetEntity,
        EntityUid? attachedEntity = null)
    {
        var emitter = SpawnEffect(effectId, coords, attachedEntity);
        if (emitter != null)
            emitter.TargetEntity = targetEntity;
        return emitter;
    }

    /// <summary>
    /// Spawns a particle effect whose emission direction points at a fixed world position.
    /// </summary>
    public ActiveEmitter? SpawnEffectAimAt(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        Vector2 targetWorldPosition,
        EntityUid? attachedEntity = null)
    {
        var emitter = SpawnEffect(effectId, coords, attachedEntity);
        if (emitter != null)
            emitter.TargetPosition = targetWorldPosition;
        return emitter;
    }

    public override void FrameUpdate(float frameTime)
    {
        _smoothedFrameTime = MathHelper.Lerp(_smoothedFrameTime, frameTime, 0.05f);

        // If particles are fully disabled, drop all emitters except those flagged to ignore quality settings.
        if (_quality == 0)
        {
            for (var i = _emitters.Count - 1; i >= 0; i--)
            {
                var e = _emitters[i];
                if (e.Proto.IgnoreQualitySettings)
                    continue;

                _liveParticleCount -= e.LiveTail;
                e.LiveTail = 0;
                e.Exhausted = true;
                RemoveEmitterAt(i);
            }
            if (_emitters.Count == 0)
                return;
        }

        if (_emitters.Count == 0)
            return;

        var eye = _eye.CurrentEye;
        var eyePos = eye.Position.Position;
        var eyeAngle = (float)eye.Rotation;
        var halfSize = new Vector2(eye.Zoom.X > 0 ? 20f / eye.Zoom.X : 20f, eye.Zoom.Y > 0 ? 15f / eye.Zoom.Y : 15f) * 1.5f;
        var viewBounds = new Box2(eyePos - halfSize, eyePos + halfSize);
        var currentMapId = eye.Position.MapId;

        _lodScale = _lodThreshold > 0
            ? Math.Clamp(MathF.Sqrt(_lodThreshold / (float)Math.Max(1, _inViewEmitters)), MinLodScale, 1f)
            : 1f;

        if (--_sortCooldown <= 0)
        {
            _sortCooldown = SortIntervalFrames;
            foreach (var e in _emitters)
            {
                var distSq = e.MapCoords.MapId == currentMapId
                    ? (e.MapCoords.Position - eyePos).LengthSquared()
                    : float.MaxValue / 2f;
                e.FramePriority = (e.Exhausted ? float.MaxValue / 4f : 0f) + distSq;
            }
            _emitters.Sort(FramePriorityComparison);
        }

        _pendingSubEmitters.Clear();
        _inViewEmitters = 0;
        // Iterate emitters in reverse so we can safely remove exhausted ones by index.
        // For each emitter: skip full simulation if off-screen (only age particles), otherwise tick it.
        // Remove any emitter that is exhausted and has no live particles left.
        for (var i = _emitters.Count - 1; i >= 0; i--)
        {
            var emitter = _emitters[i];

            // Attached emitters follow their entity every frame, even off-screen: the entity
            // may teleport, and stale coords would strand the emitter out of view permanently.
            if (emitter.AttachedEntity is { } attachedEnt)
            {
                if (Deleted(attachedEnt))
                {
                    emitter.Exhausted = true;
                    emitter.AttachedEntity = null;
                }
                else
                {
                    var attachedCoords = _transform.GetMapCoordinates(attachedEnt);
                    if (attachedCoords.MapId == MapId.Nullspace)
                    {
                        emitter.Exhausted = true;
                        emitter.AttachedEntity = null;
                    }
                    else
                    {
                        emitter.MapCoords = attachedCoords;
                        emitter.Coordinates = _transform.ToCoordinates(attachedCoords);
                    }
                }
            }
            else
            {
                // Unattached emitters refresh from the grid-relative origin so they follow moving grids.
                if (Deleted(emitter.Coordinates.EntityId))
                {
                    _liveParticleCount -= emitter.LiveTail;
                    emitter.LiveTail = 0;
                    emitter.Exhausted = true;
                    RemoveEmitterAt(i);
                    continue;
                }

                emitter.MapCoords = _transform.ToMapCoordinates(emitter.Coordinates);
            }

            var inView = emitter.MapCoords.MapId == currentMapId
                && (viewBounds.Contains(emitter.MapCoords.Position)
                    || HasVisibleParticles(emitter, viewBounds, eyeAngle));

            if (inView)
            {
                _inViewEmitters++;
                UpdateEmitterSimulation(emitter, frameTime, eyeAngle);
            }
            else
            {
                emitter.PreviousPosition = emitter.MapCoords.Position;
                AgeOffScreenParticles(emitter, frameTime);
            }

            if (emitter.Exhausted && emitter.LiveTail == 0)
                RemoveEmitterAt(i);
        }

        // Spawn any sub-emitters collected during this tick.
        // Use an index-based while loop instead of foreach because SpawnEffect can itself add
        // new entries to _pendingSubEmitters (sub-emitters of sub-emitters (dont do that)), which would
        // throw if we were iterating with an enumerator.
        var subIdx = 0;
        while (subIdx < _pendingSubEmitters.Count)
        {
            var (id, coords, depth) = _pendingSubEmitters[subIdx];
            subIdx++;
            SpawnEffect(id, coords, depth: depth);
        }
    }

    private void UpdateEmitterSimulation(ActiveEmitter emitter, float frameTime, float eyeAngle)
    {
        var interval = GetSimulationInterval();
        if (interval <= 0f)
        {
            emitter.SimulationAccumulator = 0f;
            TickEmitter(emitter, frameTime, eyeAngle);
            return;
        }

        emitter.SimulationAccumulator += frameTime;
        if (emitter.SimulationAccumulator < interval)
            return;

        // Preserve all elapsed time so reduced-frequency simulation changes smoothness, not speed.
        var simulationTime = Math.Min(emitter.SimulationAccumulator, MaxSimulationStep);
        emitter.SimulationAccumulator = Math.Min(emitter.SimulationAccumulator - simulationTime, MaxSimulationDebt);
        TickEmitter(emitter, simulationTime, eyeAngle);
    }

    private float GetSimulationInterval()
    {
        // Simulation frequency per quality level: Off 15Hz, Low 20Hz, Medium 30Hz, High every frame.
        var interval = _quality switch
        {
            <= 0 => 1f / 15f,
            1 => 1f / 20f,
            2 => 1f / 30f,
            _ => 0f,
        };

        // If the client is already struggling, give gameplay some room before particles ask for more.
        if (_smoothedFrameTime > 1f / 28f)
            return Math.Max(interval, 1f / 20f);
        if (_smoothedFrameTime > 1f / 40f)
            return Math.Max(interval, 1f / 30f);
        return interval;
    }

    private bool HasVisibleParticles(ActiveEmitter emitter, Box2 viewBounds, float eyeAngle)
    {
        // The emitter origin may sit off-screen while long-lived particles are still on it.
        for (var j = 0; j < emitter.LiveTail; j++)
        {
            if (viewBounds.Contains(ComputeParticleWorldPos(emitter.Particles[j], emitter, eyeAngle)))
                return true;
        }

        return false;
    }

    private void RemoveEmitterAt(int index)
    {
        var emitter = _emitters[index];
        _emittersByHandle.Remove(emitter.Handle);
        _emitters.RemoveAt(index);
    }

    #endregion

    #region =^..^= Emitter Internals =^..^=

     // <summary>Creates a new ActiveEmitter from a prototype and initial state.</summary>
    private ActiveEmitter? CreateEmitter(ParticleEffectPrototype proto, MapCoordinates coords, EntityUid? attached)
    {
        var emitter = new ActiveEmitter
        {
            Proto = proto,
            MapCoords = coords,
            Coordinates = _transform.ToCoordinates(coords),
            AttachedEntity = attached,
            Handle = _nextHandle++,
            SpawnOffset = proto.SpawnOffset,
            Curves = GetCurveCache(proto),
        };
        ResolveFrames(emitter);

        if (emitter.Frames.Length == 0)
            return null;

        emitter.EffectiveEmitAngle = (float)emitter.Proto.EmitAngle.Theta;
        ApplyEmitterRotation(emitter, (float)_eye.CurrentEye.Rotation);

        foreach (var _ in proto.Bursts)
            emitter.FiredBursts.Add(false);

        return emitter;
    }

    /// <summary>Stops a running emitter, preventing new particles from being emitted. Existing particles live out their lifetime.</summary>
    public void StopEffect(uint handle)
    {
        if (handle == 0)
            return;
        if (_emittersByHandle.TryGetValue(handle, out var emitter))
            emitter.Exhausted = true;
    }

    /// <summary>Stops a running emitter by direct reference. Existing particles live out their lifetime.</summary>
    public static void StopEffect(ActiveEmitter emitter) => emitter.Exhausted = true;

    /// <summary>Updates the intensity multiplier on a running emitter by handle.</summary>
    public void UpdateIntensity(uint handle, float intensity)
    {
        if (handle == 0)
            return;
        if (_emittersByHandle.TryGetValue(handle, out var emitter))
            emitter.Intensity = Math.Max(intensity, 0f);
    }

    /// <summary>Updates the intensity multiplier on a running emitter by direct reference.</summary>
    public static void UpdateIntensity(ActiveEmitter emitter, float intensity) => emitter.Intensity = Math.Max(intensity, 0f);

    private static void KillParticle(ActiveEmitter emitter, ParticleData p)
    {
        var lastIndex = emitter.LiveTail - 1;
        var index = p.ParticlesIndex;

        if (index != lastIndex)
        {
            var last = emitter.Particles[lastIndex];
            emitter.Particles[index] = last;
            emitter.Particles[lastIndex] = p;
            last.ParticlesIndex = index;
            p.ParticlesIndex = lastIndex;
        }

        emitter.LiveTail--;
    }

    private void TickEmitter(ActiveEmitter emitter, float dt, float eyeAngle)
    {
        var proto = emitter.Proto;

        // The frame loop refreshed MapCoords (and Coordinates for attached emitters) before this call.
        var newPos = emitter.MapCoords.Position;

        if (!emitter.VelocityInitialized)
        {
            emitter.PreviousPosition = newPos;
            emitter.VelocityInitialized = true;
        }

        if (dt > 0f)
            emitter.EmitterVelocity = (newPos - emitter.PreviousPosition) / dt;

        emitter.PreviousPosition = newPos;

        // Aim-at: recompute emit angle toward target each tick
        Vector2? targetWorldPos = null;
        if (emitter.TargetEntity is { } targetEnt)
        {
            if (!Deleted(targetEnt))
                targetWorldPos = _transform.GetMapCoordinates(targetEnt).Position;
            else
                emitter.TargetEntity = null; // entity GONE, fall to TargetPosition
        }
        if (targetWorldPos == null && emitter.TargetPosition.HasValue)
            targetWorldPos = emitter.TargetPosition.Value;

        if (targetWorldPos.HasValue)
        {
            var worldDir = targetWorldPos.Value - emitter.MapCoords.Position;
            if (worldDir.LengthSquared() > 0.0001f)
            {
                // Convert world direction to screen-space direction to angle (0 = screen-up)
                var screenDirection = RotateVector(worldDir, eyeAngle);
                emitter.EffectiveEmitAngle = MathF.Atan2(screenDirection.X, screenDirection.Y);
            }
        }
        else
        {
            // No target, keep in sync with the overridden emit angle if set, otherwise prototype default
            var baseAngle = emitter.Overrides?.EmitAngle ?? emitter.Proto.EmitAngle;
            emitter.EffectiveEmitAngle = (float)baseAngle.Theta;
            ApplyEmitterRotation(emitter, eyeAngle);
        }

        // Resolve overridable scalars once per tick
        // ᓚᘏᗢ <( look how pretty the formatting is :)
        var ovr          = emitter.Overrides;
        var drag         = ovr?.Drag          ?? proto.Drag;
        var constForce   = ovr?.ConstantForce ?? proto.ConstantForce;
        var termSpeed    = ovr?.TerminalSpeed ?? proto.TerminalSpeed;
        var gravity      = ovr?.Gravity       ?? proto.Gravity;
        var noiseStr     = ovr?.NoiseStrength ?? proto.NoiseStrength;
        var noiseFreq    = ovr?.NoiseFrequency ?? proto.NoiseFrequency;
        var duration     = (float)(ovr?.Duration      ?? proto.Duration).TotalSeconds;
        var emissionRate = ovr?.EmissionRate  ?? proto.EmissionRate;
        var maxCount     = ResolveMaxCount(emitter);

        if (proto.RotateWithEmitter && constForce != Vector2.Zero &&
            emitter.AttachedEntity is { } forceEnt && !Deleted(forceEnt))
        {
            // Directional effects define force in emitter-local space. Convert it to the
            // screen-space simulation coordinates; the draw path later applies the grid transform.
            var localForce = Transform(forceEnt).LocalRotation.RotateVec(constForce);
            constForce = RotateVector(localForce, eyeAngle);
        }

        // Precompute per-tick constants for SimulateParticle to avoid recomputing per particle.
        var dragMul     = drag > 0f ? MathF.Exp(-drag * dt) : 1f;
        var termSpeedSq = termSpeed > 0f ? termSpeed * termSpeed : float.MaxValue;

        // Advance age and check duration
        emitter.Age += TimeSpan.FromSeconds(dt);
        if (!emitter.Exhausted && duration > 0f && emitter.Age.TotalSeconds >= duration)
            emitter.Exhausted = true;

        // RSI animation
        if (emitter.Delays.Length > 0 && emitter.Frames.Length > 0)
        {
            emitter.AnimTimer += dt;
            while (emitter.AnimTimer >= emitter.Delays[emitter.AnimFrame])
            {
                var delay = emitter.Delays[emitter.AnimFrame];
                if (delay <= 0f)
                    break;
                emitter.AnimTimer -= delay;
                emitter.AnimFrame = (emitter.AnimFrame + 1) % emitter.Frames.Length;
            }
        }

        // Simulate live particles
        var liveCount = emitter.LiveTail;
        for (var i = 0; i < emitter.LiveTail; i++)
        {
            var p = emitter.Particles[i];
            p.Age += dt;
            p.UpdateCurveSample();

            if (p.Age >= p.Lifetime)
            {
                if (proto.SubEmitterOnDeath.HasValue)
                {
                    var worldPos = ComputeParticleWorldPos(p, emitter, eyeAngle);
                    _pendingSubEmitters.Add((proto.SubEmitterOnDeath.Value,
                        new MapCoordinates(worldPos, emitter.MapCoords.MapId),
                        emitter.SubEmitterDepth + 1));
                }

                _liveParticleCount--;
                KillParticle(emitter, p);
                i--;
                continue;
            }

            SimulateParticle(p, dt, dragMul, constForce, termSpeed, termSpeedSq, gravity, noiseStr, noiseFreq, emitter.Curves);
        }

        // Timed bursts
        if (!emitter.Exhausted)
        {
            for (int b = 0; b < proto.Bursts.Count; b++)
            {
                if (emitter.FiredBursts[b])
                    continue;
                var burst = proto.Bursts[b];
                if (emitter.Age < burst.Time)
                    continue;

                // Bypass quality settings for gameplay-critical particles; auto-LOD never scales those either.
                var qualityMult = EmissionQualityMultiplier(proto);
                var effectiveMax = proto.IgnoreQualitySettings && _quality < 3
                    ? Math.Min(maxCount, IgnoreQualityMaxParticles)
                    : maxCount;
                var scaledMax = (int)Math.Ceiling(Math.Min(effectiveMax, HardMaxParticles) * qualityMult * emitter.Intensity);
                var toEmit = (int)Math.Ceiling(burst.Count * qualityMult * emitter.Intensity);
                for (int j = 0; j < toEmit && _liveParticleCount < _globalBudget && liveCount < scaledMax; j++)
                {
                    EmitParticle(emitter, eyeAngle);
                    liveCount++;
                }
                emitter.FiredBursts[b] = true;
            }

            liveCount = emitter.LiveTail;
        }

        // Continuous emission
        if (!emitter.Exhausted && !proto.Burst)
        {
            var qualityMult = EmissionQualityMultiplier(proto);
            // IgnoreQualitySettings emitters are capped at IgnoreQualityMaxParticles unless quality is High.
            var effectiveMax = proto.IgnoreQualitySettings && _quality < 3
                ? Math.Min(maxCount, IgnoreQualityMaxParticles)
                : maxCount;
            var scaledMax = (int)Math.Ceiling(Math.Min(effectiveMax, HardMaxParticles) * qualityMult * emitter.Intensity);
            var available = _globalBudget - _liveParticleCount;
            var canEmit = Math.Min(scaledMax - liveCount, available);
            if (canEmit > 0)
            {
                // EmissionOverTime rate multiplier
                float emissionMult = 1f;
                if (emitter.Curves.Emission is { } emissionCurve)
                {
                    var t = duration > 0f
                        ? Math.Clamp((float)(emitter.Age.TotalSeconds / duration), 0f, 1f)
                        : Math.Clamp((float)emitter.Age.TotalSeconds, 0f, 1f);
                    emissionMult = ParticleCurveCache.Sample(emissionCurve, t);
                }

                emitter.EmitAccum += emissionRate * emissionMult * dt * qualityMult * emitter.Intensity;
                int toEmit = (int)emitter.EmitAccum;
                emitter.EmitAccum -= toEmit;
                toEmit = Math.Min(toEmit, canEmit);

                for (int i = 0; i < toEmit; i++)
                    EmitParticle(emitter, eyeAngle);
            }
        }

        if (proto.Burst && !emitter.Exhausted)
            emitter.Exhausted = true;
    }

    private float EmissionQualityMultiplier(ParticleEffectPrototype proto)
    {
        if (proto.IgnoreQualitySettings)
            return 1f;

        return QualityMultipliers[Math.Clamp(_quality, 0, QualityMultipliers.Length - 1)]
            * _lodScale;
    }

    private void ApplyEmitterRotation(ActiveEmitter emitter, float eyeAngle)
    {
        if (!emitter.Proto.RotateWithEmitter ||
            emitter.AttachedEntity is not { } rotatingEnt ||
            Deleted(rotatingEnt))
            return;

        // Stay in grid-local space here; rendering applies the grid's live world rotation.
        var localDirection = Transform(rotatingEnt).LocalRotation.RotateVec(Vector2.UnitY);
        var screenDirection = RotateVector(localDirection, eyeAngle);
        emitter.EffectiveEmitAngle += MathF.Atan2(screenDirection.X, screenDirection.Y);
    }

    private void BurstEmit(ActiveEmitter emitter)
    {
        var proto = emitter.Proto;
        var eyeAngle = (float)_eye.CurrentEye.Rotation;
        // Bypass quality settings for gameplay-critical particles
        var qualityMult = EmissionQualityMultiplier(proto);
        // IgnoreQualitySettings emitters are capped at IgnoreQualityMaxParticles to prevent performance issues or otherwise abuse.
        var effectiveMax = proto.IgnoreQualitySettings && _quality < 3
            ? Math.Min(ResolveMaxCount(emitter), IgnoreQualityMaxParticles)
            : ResolveMaxCount(emitter);
        var count = (int)Math.Ceiling(Math.Min(effectiveMax, HardMaxParticles) * qualityMult * emitter.Intensity);
        for (int i = 0; i < count && _liveParticleCount < _globalBudget; i++)
            EmitParticle(emitter, eyeAngle);
    }

    private void EmitParticle(ActiveEmitter emitter, float eyeAngle)
    {
        var proto = emitter.Proto;

        ParticleData p;
        if (emitter.LiveTail < emitter.Particles.Count)
        {
            p = emitter.Particles[emitter.LiveTail];
            p.Reset();
            p.ParticlesIndex = emitter.LiveTail;
        }
        else
        {
            p = new ParticleData();
            p.ParticlesIndex = emitter.Particles.Count;
            emitter.Particles.Add(p);
        }

        emitter.LiveTail++;

        // Resolve spawn time overridable fields
        _liveParticleCount++;
        var ovr = emitter.Overrides;
        var lifetime        = (float)(ovr?.Lifetime         ?? proto.Lifetime).TotalSeconds;
        var lifetimeVar     = (float)(ovr?.LifetimeVariance  ?? proto.LifetimeVariance).TotalSeconds;
        var spreadAngle     = (float)(ovr?.SpreadAngle?.Theta     ?? proto.SpreadAngle.Theta);
        var speed0          = ovr?.Speed             ?? proto.Speed;
        var speedVar        = ovr?.SpeedVariance     ?? proto.SpeedVariance;
        var sizeVar         = ovr?.SizeVariance      ?? proto.SizeVariance;
        var inheritVel      = ovr?.InheritVelocity   ?? proto.InheritVelocity;
        var startRot        = (float)(ovr?.StartRotation?.Theta         ?? proto.StartRotation.Theta);
        var startRotVar     = (float)(ovr?.StartRotationVariance?.Theta ?? proto.StartRotationVariance.Theta);
        var rotSpeed        = (float)(ovr?.RotationSpeed?.Theta         ?? proto.RotationSpeed.Theta);
        var rotSpeedVar     = (float)(ovr?.RotationSpeedVariance?.Theta ?? proto.RotationSpeedVariance.Theta);

        p.Lifetime = MathF.Max(lifetime + _random.NextFloat(-lifetimeVar, lifetimeVar), 0.05f);

        var spreadHalf = spreadAngle * 0.5f;
        var angle = emitter.EffectiveEmitAngle + _random.NextFloat(-spreadHalf, spreadHalf);

        var speed = speed0 + _random.NextFloat(-speedVar, speedVar);
        speed = Math.Max(speed, 0f);

        p.Velocity = new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * speed;
        p.LocalOffset = SampleEmissionShape(proto.Shape);

        // Local effects carry their spawn offset in LocalOffset. Map/grid effects bake it into
        // their captured spawn origin below so it is not applied twice.
        var spawnOffset = emitter.Overrides?.SpawnOffset ?? emitter.SpawnOffset;
        var simulationSpace = proto.ResolvedSimulationSpace;
        if (simulationSpace == ParticleSimulationSpace.Local && spawnOffset != default)
        {
            p.LocalOffset += RotateVector(spawnOffset, eyeAngle);
        }

        // InheritVelocity: convert emitter world velocity to screen space then add
        if (inheritVel != 0f && emitter.EmitterVelocity != Vector2.Zero)
        {
            var wv = emitter.EmitterVelocity * inheritVel;
            p.Velocity += RotateVector(wv, eyeAngle);
        }

        if (simulationSpace != ParticleSimulationSpace.Local)
        {
            if (proto.RotateWithEmitter && emitter.AttachedEntity is { } offsetEnt && !Deleted(offsetEnt))
            {
                // SpawnOffset is local to the attached entity for directional effects. Because
                // these coordinates are relative to the grid, shuttle rotation moves the nozzle
                // position automatically on every subsequent draw.
                var localOffset = Transform(offsetEnt).LocalRotation.RotateVec(spawnOffset);
                p.SpawnCoordinates = new EntityCoordinates(
                    emitter.Coordinates.EntityId,
                    emitter.Coordinates.Position + localOffset);
            }
            else
            {
                var spawnCoords = new MapCoordinates(
                    emitter.MapCoords.Position + spawnOffset,
                    emitter.MapCoords.MapId);
                p.SpawnCoordinates = _transform.ToCoordinates(spawnCoords);
            }

            p.SpawnMapPosition = _transform.ToMapCoordinates(p.SpawnCoordinates).Position;
        }

        p.SpawnSpeed = speed;
        p.SpawnIntensity = emitter.Intensity;

        // SizeVariance
        if (sizeVar > 0f)
            p.SizeMultiplier = 1f + _random.NextFloat(-sizeVar, sizeVar);
        else
            p.SizeMultiplier = 1f;

        p.Rotation = startRot + _random.NextFloat(-startRotVar, startRotVar);
        p.RotationSpeed = rotSpeed + _random.NextFloat(-rotSpeedVar, rotSpeedVar);

        // Unique noise offset so each particle gets different turbulence
        p.NoiseOffset = new Vector2(_random.NextFloat(-100f, 100f), _random.NextFloat(-100f, 100f));

        // Sub-emitter on spawn
        if (proto.SubEmitterOnSpawn.HasValue)
        {
            var worldPos = ComputeParticleWorldPos(p, emitter, eyeAngle);
            _pendingSubEmitters.Add((proto.SubEmitterOnSpawn.Value,
                new MapCoordinates(worldPos, emitter.MapCoords.MapId),
                emitter.SubEmitterDepth + 1));
        }
    }

    #endregion

    #region =^..^= Helpers =^..^=

    /// <summary>
    /// Advances a single live particle's simulation by one step.
    /// </summary>
    private static void SimulateParticle(
        ParticleData p,
        float dt,
        float dragMul,
        Vector2 constForce,
        float termSpeed,
        float termSpeedSq,
        float gravity,
        float noiseStr,
        float noiseFreq,
        ParticleCurveCache curves)
    {
        var ageRatio = p.AgeRatio;

        // Drag: dragMul is MathF.Exp(-drag * dt) precomputed per tick
        if (dragMul < 1f)
            p.Velocity *= dragMul;

        // ConstantForce
        if (constForce != Vector2.Zero)
            p.Velocity += constForce * dt;

        // ForceOverLifetime
        if (curves.Force is { } forceCurve)
            p.Velocity += ParticleCurveCache.Sample(forceCurve, p.CurveIndex, p.CurveLerp) * dt;

        // SpeedOverLifetime: rescale velocity magnitude to the curve-defined speed
        if (curves.Speed is { } speedCurve)
        {
            var curveSpeed = ParticleCurveCache.Sample(speedCurve, p.CurveIndex, p.CurveLerp) * p.SpawnSpeed;
            var currentSpeed = p.Velocity.Length();
            if (currentSpeed > 0f)
                p.Velocity = p.Velocity / currentSpeed * curveSpeed;
        }

        // Terminal speed cap: termSpeedSq is termSpeed*termSpeed precomputed per tick
        if (termSpeedSq < float.MaxValue)
        {
            var speedSq = p.Velocity.LengthSquared();
            if (speedSq > termSpeedSq)
                p.Velocity *= termSpeed / MathF.Sqrt(speedSq);
        }

        // Advance position
        p.LocalOffset += p.Velocity * dt;

        // VelocityOverLifetime: positional nudge (does not modify velocity)
        if (curves.Velocity is { } velocityCurve)
            p.LocalOffset += ParticleCurveCache.Sample(velocityCurve, p.CurveIndex, p.CurveLerp) * dt;

        // Gravity
        if (gravity != 0f)
            p.LocalOffset.Y += -gravity * dt * ageRatio;

        // Noise
        if (noiseStr > 0f)
        {
            var nx = ValueNoise(p.NoiseOffset.X + p.Age * noiseFreq, p.NoiseOffset.Y);
            var ny = ValueNoise(p.NoiseOffset.X, p.NoiseOffset.Y + p.Age * noiseFreq);
            p.LocalOffset += new Vector2(nx, ny) * noiseStr * dt;
        }

        // SPIN!!!!
        if (p.RotationSpeed != 0f)
            p.Rotation += p.RotationSpeed * dt;
    }

    /// <summary>Converts a particle's screen-space LocalOffset to a world position.</summary>
    private Vector2 ComputeParticleWorldPos(ParticleData p, ActiveEmitter emitter, float eyeAngle)
    {
        var worldOffset = RotateVector(p.LocalOffset, -eyeAngle);
        switch (emitter.Proto.ResolvedSimulationSpace)
        {
            case ParticleSimulationSpace.Map:
                return p.SpawnMapPosition + worldOffset;
            case ParticleSimulationSpace.Grid:
                var particleCoords = new EntityCoordinates(
                    p.SpawnCoordinates.EntityId,
                    p.SpawnCoordinates.Position + worldOffset);
                return Deleted(particleCoords.EntityId)
                    ? emitter.MapCoords.Position
                    : _transform.ToMapCoordinates(particleCoords).Position;
            default:
                return emitter.MapCoords.Position + worldOffset;
        }
    }

    /// <summary>
    /// Ages particles on off-screen emitters without running full simulation.
    /// Kills expired particles and decrements the live count.
    /// </summary>
    private void AgeOffScreenParticles(ActiveEmitter emitter, float dt)
    {
        emitter.Age += TimeSpan.FromSeconds(dt);
        var duration = emitter.Overrides?.Duration ?? emitter.Proto.Duration;
        if (!emitter.Exhausted && duration > TimeSpan.Zero && emitter.Age >= duration)
            emitter.Exhausted = true;

        if (!emitter.Exhausted && emitter.Proto.Burst && emitter.Proto.Bursts.Count == 0)
            emitter.Exhausted = true;

        for (var i = 0; i < emitter.LiveTail; i++)
        {
            var p = emitter.Particles[i];
            p.Age += dt;
            p.UpdateCurveSample();

            if (p.Age < p.Lifetime)
                continue;

            _liveParticleCount--;
            KillParticle(emitter, p);
            i--;
        }
    }

    private int ResolveMaxCount(ActiveEmitter emitter)
    {
        if (emitter.Overrides?.MaxCount is { } runtimeMax)
            return runtimeMax;
        if (emitter.Proto.MaxCount is { } protoMax)
            return protoMax;
        return CalculateMaxCount(emitter.Proto, emitter.Overrides);
    }

    private static int CalculateMaxCount(ParticleEffectPrototype prototype, ParticleRuntimeOverrides? overrides = null)
    {
        if (prototype.Burst)
            return DefaultBurstMaxCount;

        var lifetimeSpan = (overrides?.Lifetime ?? prototype.Lifetime)
            + (overrides?.LifetimeVariance ?? prototype.LifetimeVariance);
        var lifetime = Math.Max(lifetimeSpan.Duration().TotalSeconds, 0.05);

        var peakEmissionMultiplier = prototype.EmissionOverTime.Count == 0 ? 1f : 0f;
        foreach (var key in prototype.EmissionOverTime)
            peakEmissionMultiplier = Math.Max(peakEmissionMultiplier, key.Value);

        var continuousCount = (int)Math.Ceiling(
            Math.Max(overrides?.EmissionRate ?? prototype.EmissionRate, 0f) * peakEmissionMultiplier * lifetime);

        // Timed bursts can overlap both continuous particles and one another. Adding them all is
        // intentionally conservative; the global budget and hard ceiling still get the final say.
        long burstCount = 0;
        foreach (var burst in prototype.Bursts)
            burstCount += Math.Max(burst.Count, 0);

        return (int)Math.Clamp(continuousCount + burstCount, 1L, AutomaticMaxCount);
    }

    private void ResolveFrames(ActiveEmitter emitter)
    {
        var protoId = emitter.Proto.ID;

        if (_frameCache.TryGetValue(protoId, out var cached))
        {
            emitter.Frames = cached.Frames;
            emitter.Delays = cached.Delays;
            return;
        }

        if (_frameResolveFailures.Contains(protoId))
            return;

        Texture[] frames = Array.Empty<Texture>();
        float[] delays = Array.Empty<float>();

        switch (emitter.Proto.Sprite)
        {
            case SpriteSpecifier.Rsi rsi:
            {
                RSI? resource;
                try
                {
                    var path = rsi.RsiPath.IsRooted
                        ? rsi.RsiPath
                        : SpriteSpecifierSerializer.TextureRoot / rsi.RsiPath;
                    resource = _resourceCache.GetResource<RSIResource>(path).RSI;
                }
                catch (Exception e)
                {
                    _frameResolveFailures.Add(protoId);
                    Log.Warning($"ParticleSystem: failed to load RSI for '{protoId}': {e.Message}");
                    return;
                }

                if (!resource.TryGetState(rsi.RsiState, out var state))
                {
                    _frameResolveFailures.Add(protoId);
                    Log.Warning($"ParticleSystem: RSI state '{rsi.RsiState}' missing in '{rsi.RsiPath}' for '{protoId}'.");
                    return;
                }

                frames = state.GetFrames(RsiDirection.South);
                delays = state.GetDelays();
                break;
            }
            case SpriteSpecifier.Texture tex:
            {
                try { frames = new[] { _spriteSystem.Frame0(tex) }; }
                catch (Exception e)
                {
                    _frameResolveFailures.Add(protoId);
                    Log.Warning($"ParticleSystem: failed to load texture for '{protoId}': {e.Message}");
                    return;
                }
                break;
            }
            default:
                _frameResolveFailures.Add(protoId);
                Log.Warning($"ParticleSystem: unsupported sprite specifier for '{protoId}'.");
                return;
        }

        _frameCache[protoId] = (frames, delays);
        emitter.Frames = frames;
        emitter.Delays = delays;
    }

    private Vector2 SampleEmissionShape(EmissionShapeData shape)
    {
        switch (shape.Type)
        {
            case EmissionShapeType.Point:
                return Vector2.Zero;
            case EmissionShapeType.CircleEdge:
            {
                var a = _random.NextFloat(0f, MathF.PI * 2f);
                return new Vector2(MathF.Cos(a), MathF.Sin(a)) * shape.Radius;
            }
            case EmissionShapeType.CircleFill:
            {
                var a = _random.NextFloat(0f, MathF.PI * 2f);
                var r = shape.Radius * MathF.Sqrt(_random.NextFloat(0f, 1f));
                return new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
            }
            case EmissionShapeType.Box:
            {
                return new Vector2(_random.NextFloat(-shape.BoxExtents.X, shape.BoxExtents.X),
                                   _random.NextFloat(-shape.BoxExtents.Y, shape.BoxExtents.Y));
            }
            default:
                return Vector2.Zero;
        }
    }

    #endregion

    // Curve samplers, the baked-curve cache and the noise table moved to ParticleSystem.Curves.cs.
}