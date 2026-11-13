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

    private static float ComputeIntensity(float stacks)
        => Math.Clamp(1f + stacks / MaxStacks, 1f, 2f);

    private float ResolveStacks(EntityUid uid, FlammableComponent comp)
    {
        if (comp.FireStacks > 0f)
            return comp.FireStacks;

        if (_appearance.TryGetData<float>(uid, FireVisuals.FireStacks, out var appearanceStacks))
            return appearanceStacks;

        return 0f;
    }

    private void OnAppearanceChange(Entity<FlammableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, FireVisuals.OnFire, out bool onFire))
            onFire = false;

        var stacks = ResolveStacks(ent.Owner, ent.Comp);

        if (onFire)
        {
            if (!_active.TryGetValue(ent, out var state))
            {
                var coords = _transform.GetMapCoordinates(ent);
                var fire = _particles.SpawnEffect(FireEffect, coords, ent.Owner);
                var smoke = _particles.SpawnEffect(SmokeEffect, coords, ent.Owner);

                if (fire != null || smoke != null)
                {
                    state = new FireState { FireEmitter = fire, SmokeEmitter = smoke };
                    _active[ent] = state;
                }
            }
            else
            {
                var needsFire = state.FireEmitter == null || state.FireEmitter.Exhausted;
                var needsSmoke = state.SmokeEmitter == null || state.SmokeEmitter.Exhausted;

                if (needsFire || needsSmoke)
                {
                    var coords = _transform.GetMapCoordinates(ent);

                    if (needsFire)
                    {
                        var fresh = _particles.SpawnEffect(FireEffect, coords, ent.Owner);
                        if (fresh != null)
                            state.FireEmitter = fresh;
                    }

                    if (needsSmoke)
                    {
                        var fresh = _particles.SpawnEffect(SmokeEffect, coords, ent.Owner);
                        if (fresh != null)
                            state.SmokeEmitter = fresh;
                    }
                }
            }

            // Update intensity on live emitters
            if (state != null)
            {
                var intensity = ComputeIntensity(stacks);
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
        while (enumerator.MoveNext(out var uid, out var flammable, out _))
        {
            if (!_appearance.TryGetData(uid, FireVisuals.OnFire, out bool onFire) || !onFire)
                continue;

            var stacks = ResolveStacks(uid, flammable);

            _active.TryGetValue(uid, out var state);

            state?.FireEmitter?.Exhausted = true;
            state?.SmokeEmitter?.Exhausted = true;

            var intensity = ComputeIntensity(stacks);

            var coords = _transform.GetMapCoordinates(uid);
            var fire = _particles.SpawnEffect(FireEffect, coords, uid);
            var smoke = _particles.SpawnEffect(SmokeEffect, coords, uid);

            if (fire != null)
            {
                fire.Intensity = intensity;
                if (state != null)
                    state.FireEmitter = fire;
            }

            if (smoke != null)
            {
                smoke.Intensity = intensity;
                if (state != null)
                    state.SmokeEmitter = smoke;
            }

            if (state == null && (fire != null || smoke != null))
                _active[uid] = new FireState { FireEmitter = fire, SmokeEmitter = smoke };
        }
    }
}