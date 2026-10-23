using Content.Shared.ADT.Particles;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Spawns a particle effect on this client when an entity with
/// <see cref="ParticleEmitterComponent"/> is initialized (including on PVS re-entry).
/// </summary>
public sealed class ParticleEmitterSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Track emitter references so we can stop them when the entity leaves PVS or is removed.
    private readonly Dictionary<EntityUid, ActiveEmitter> _activeEmitters = new();

    private readonly List<EntityUid> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ParticleEmitterComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<ParticleSettingsChangedEvent>(OnSettingsChanged);
    }

    private void OnCompInit(Entity<ParticleEmitterComponent> ent, ref ComponentInit args)
    {
        // Stop any lingering emitter from a previous PVS cycle for this entity.
        if (_activeEmitters.Remove(ent.Owner, out var old))
            _particles.RemoveParticle(old);

        var xform = Transform(ent.Owner);
        if (xform.MapUid == null)
        {
            _pending.Add(ent.Owner);
            return;
        }

        ApplyEmitter(ent.Owner, ent.Comp, xform.MapPosition);
    }

    private void OnCompShutdown(Entity<ParticleEmitterComponent> ent, ref ComponentShutdown args)
    {
        _pending.Remove(ent.Owner);
        if (_activeEmitters.Remove(ent.Owner, out var emitter))
            _particles.RemoveParticle(emitter);
    }

    private void OnSettingsChanged(ParticleSettingsChangedEvent ev)
    {
        if (ev.Quality == 0)
        {
            _pending.Clear();
            return;
        }

        var enumerator = EntityQueryEnumerator<ParticleEmitterComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (_activeEmitters.TryGetValue(uid, out var existing) && !existing.Exhausted)
                continue;

            _activeEmitters.Remove(uid);
            _pending.Add(uid);
        }
    }

    public override void Update(float frameTime)
    {
        if (_pending.Count == 0 || !_timing.IsFirstTimePredicted)
            return;

        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var uid = _pending[i];
            if (!TryComp<ParticleEmitterComponent>(uid, out var comp))
            {
                _pending.RemoveAt(i);
                continue;
            }

            var xform = Transform(uid);
            if (xform.MapUid == null)
                continue;

            ApplyEmitter(uid, comp, xform.MapPosition);
            _pending.RemoveAt(i);
        }
    }

    private bool ApplyEmitter(EntityUid uid, ParticleEmitterComponent comp, MapCoordinates coords)
    {
        if (_activeEmitters.TryGetValue(uid, out var existing) && !existing.Exhausted)
            return true;

        if (existing != null)
            _activeEmitters.Remove(uid);

        var emitter = _particles.SpawnEffect(comp.Effect, coords, uid, comp.ColorOverride);
        if (emitter == null)
        {
            return false;
        }

        if (comp.Intensity != 1f)
            emitter.Intensity = comp.Intensity;

        if (comp.SpawnOffset != default)
            emitter.SpawnOffset = comp.SpawnOffset;

        _activeEmitters[uid] = emitter;
        return true;
    }
}
