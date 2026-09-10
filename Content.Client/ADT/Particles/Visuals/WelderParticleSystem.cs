using Content.Shared.ADT.Particles;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Sparks while something is being welded with a welding tool: closing lockers, airlocks, cutting walls,
/// repairing borgs and structures, construction and so on.
/// Tool do-afters raise <see cref="DoAfterAttemptEvent{T}"/> every tick while they run, so the sparks follow the
/// weld. The effect has a short safety duration and is restarted on each heartbeat, so it dies on its own when the
/// do-after ends.
/// </summary>
public sealed class WelderParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> SparksEffect = "ADTWeldingSparks";

    // Short safety duration so leaked effects (e.g. silent prediction rollbacks) die on their own.
    private static readonly ParticleRuntimeOverrides SparksOverrides = new()
    {
        Duration = TimeSpan.FromSeconds(3)
    };

    private readonly Dictionary<DoAfterId, ActiveEmitter> _active = new();
    private readonly List<DoAfterId> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToolComponent, DoAfterAttemptEvent<SharedToolSystem.ToolDoAfterEvent>>(OnToolAttempt);
        SubscribeLocalEvent<ActiveDoAfterComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnToolAttempt(Entity<ToolComponent> tool, ref DoAfterAttemptEvent<SharedToolSystem.ToolDoAfterEvent> args)
    {
        if (!_toolSystem.HasQuality(tool, SharedToolSystem.WeldingQuality))
            return;

        var doAfter = args.DoAfter;
        if (_active.TryGetValue(doAfter.Id, out var emitter))
        {
            if (!emitter.Exhausted)
                return;

            // Weld is still going but the effect hit its safety duration; restart it.
            _particles.RemoveParticle(emitter);
            _active.Remove(doAfter.Id);
        }

        if (doAfter.Args.Target is not { } target)
            return;

        var coords = _transform.GetMapCoordinates(target);
        emitter = _particles.SpawnEffect(SparksEffect, coords, target, overrides: SparksOverrides);
        if (emitter != null)
            _active[doAfter.Id] = emitter;
    }

    private void OnActiveShutdown(Entity<ActiveDoAfterComponent> ent, ref ComponentShutdown args)
    {
        _toRemove.Clear();
        foreach (var (id, emitter) in _active)
        {
            if (id.Uid != ent.Owner)
                continue;

            _particles.RemoveParticle(emitter);
            _toRemove.Add(id);
        }

        foreach (var id in _toRemove)
            _active.Remove(id);
    }
}