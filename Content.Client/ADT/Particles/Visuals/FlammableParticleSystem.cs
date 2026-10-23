using Content.Shared.ADT.Particles;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Particles when entities are on fire.
/// </summary>
public sealed class FlammableParticleSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> FireEffect  = "ADTFireContinuous";
    private static readonly ProtoId<ParticleEffectPrototype> SmokeEffect = "ADTFireSmoke";

    private const float MaxStacks = 10f;

    private sealed class FireState
    {
        public ActiveEmitter? FireEmitter;
        public ActiveEmitter? SmokeEmitter;
    }

    private readonly Dictionary<EntityUid, FireState> _active = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlammableComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<FlammableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ParticleSettingsChangedEvent>(OnSettingsChanged);
    }

    private void OnAppearanceChange(Entity<FlammableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, FireVisuals.OnFire, out bool onFire))
            onFire = false;

        if (!_appearance.TryGetData(ent, FireVisuals.FireStacks, out float stacks))
            stacks = 0f;

        if (onFire)
        {
            var coords = _transform.GetMapCoordinates(ent);

            if (!_active.TryGetValue(ent, out var state))
            {
                state = new FireState
                {
                    SmokeEmitter = _particles.SpawnEffect(SmokeEffect, coords, ent.Owner),
                    FireEmitter  = _particles.SpawnEffect(FireEffect,  coords, ent.Owner),
                };
                _active[ent] = state;
            }
            else
            {
                if (state.FireEmitter == null || state.FireEmitter.Exhausted)
                {
                    var fresh = _particles.SpawnEffect(FireEffect, coords, ent.Owner);
                    if (fresh != null)
                        state.FireEmitter = fresh;
                }
                if (state.SmokeEmitter == null || state.SmokeEmitter.Exhausted)
                {
                    var fresh = _particles.SpawnEffect(SmokeEffect, coords, ent.Owner);
                    if (fresh != null)
                        state.SmokeEmitter = fresh;
                }
            }

            // Update intensity on live emitters
            if (state.FireEmitter != null || state.SmokeEmitter != null)
            {
                var intensity = Math.Clamp(stacks / MaxStacks * 2f, 1f, 2f);
                if (state.FireEmitter != null)
                    state.FireEmitter.Intensity = intensity;
                if (state.SmokeEmitter != null)
                    state.SmokeEmitter.Intensity = intensity;
            }
        }
        else if (_active.Remove(ent, out var state))
        {
            state.FireEmitter?.Exhausted = true;
            state.SmokeEmitter?.Exhausted = true;
        }
    }

    private void OnShutdown(Entity<FlammableComponent> ent, ref ComponentShutdown args)
    {
        if (_active.Remove(ent, out var state))
        {
            state.FireEmitter?.Exhausted = true;
            state.SmokeEmitter?.Exhausted = true;
        }
    }

    private void OnSettingsChanged(ParticleSettingsChangedEvent ev)
    {
        if (ev.Quality == 0)
            return;

        var enumerator = EntityQueryEnumerator<FlammableComponent, AppearanceComponent>();
        while (enumerator.MoveNext(out var uid, out _, out _))
        {
            if (!_appearance.TryGetData(uid, FireVisuals.OnFire, out bool onFire) || !onFire)
                continue;

            _active.TryGetValue(uid, out var state);

            state?.FireEmitter?.Exhausted = true;
            state?.SmokeEmitter?.Exhausted = true;

            var coords = _transform.GetMapCoordinates(uid);
            var fire = _particles.SpawnEffect(FireEffect, coords, uid);
            var smoke = _particles.SpawnEffect(SmokeEffect, coords, uid);

            if (state != null)
            {
                if (fire != null)
                    state.FireEmitter = fire;
                if (smoke != null)
                    state.SmokeEmitter = smoke;
            }
            else if (fire != null || smoke != null)
            {
                _active[uid] = new FireState { FireEmitter = fire, SmokeEmitter = smoke };
            }
        }
    }
}
