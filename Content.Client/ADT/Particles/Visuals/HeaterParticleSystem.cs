using System.Numerics;
using Content.Shared.ADT.Particles;
using Content.Shared.Kitchen.Components;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Particles;

/// <summary>
/// Fire particles on the food being cooked: items placed on an electric heater (grills, ranges)
/// while it is switched on and powered. Electric ranges also get fire and smoke on the stove
/// itself while their oven is cooking. Plain microwaves have no EntityHeater, so they are unaffected.
/// </summary>
public sealed class HeaterParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> FireEffect = "ADTFireContinuous";
    private static readonly ProtoId<ParticleEffectPrototype> SmokeEffect = "ADTFireSmoke";
    private static readonly Vector2 SpawnOffset = new(0f, 0.0f); // TODO: for the future

    private sealed class HeaterState
    {
        public bool On;
        public readonly Dictionary<EntityUid, ActiveEmitter> Emitters = new();
    }

    private readonly Dictionary<EntityUid, HeaterState> _heaters = new();
    private readonly Dictionary<EntityUid, ActiveEmitter> _fire = new();
    private readonly Dictionary<EntityUid, ActiveEmitter> _smoke = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityHeaterComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ItemPlacerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ItemPlacerComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<EntityHeaterComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAppearanceChange(Entity<EntityHeaterComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, EntityHeaterVisuals.Setting, out EntityHeaterSetting setting))
            setting = EntityHeaterSetting.Off;

        if (!_heaters.TryGetValue(ent, out var state))
        {
            state = new HeaterState();
            _heaters[ent] = state;
        }

        if (setting != EntityHeaterSetting.Off)
        {
            state.On = true;
            if (TryComp(ent, out ItemPlacerComponent? placer))
            {
                foreach (var item in placer.PlacedEntities)
                    SpawnOnItem(item, state);
            }
        }
        else
        {
            state.On = false;
            StopAll(state);
        }

        if (!_appearance.TryGetData(ent, PowerDeviceVisuals.VisualState, out MicrowaveVisualState visualState))
            visualState = MicrowaveVisualState.Idle;

        if (visualState == MicrowaveVisualState.Cooking)
        {
            var fireAlive = _fire.TryGetValue(ent, out var fire) && !fire.Exhausted;
            var smokeAlive = _smoke.TryGetValue(ent, out var smoke) && !smoke.Exhausted;

            if (fireAlive && smokeAlive)
                return;

            _fire.Remove(ent);
            _smoke.Remove(ent);

            var coords = _transform.GetMapCoordinates(ent);
            fire = _particles.SpawnEffect(FireEffect, coords, ent.Owner);
            smoke = _particles.SpawnEffect(SmokeEffect, coords, ent.Owner);

            if (fire != null)
            {
                fire.SpawnOffset = SpawnOffset;
                _fire[ent.Owner] = fire;
            }

            if (smoke != null)
            {
                smoke.SpawnOffset = SpawnOffset;
                _smoke[ent.Owner] = smoke;
            }
        }
        else
        {
            if (_fire.Remove(ent, out var fire))
                _particles.RemoveParticle(fire);

            if (_smoke.Remove(ent, out var smoke))
                _particles.RemoveParticle(smoke);
        }
    }

    private void OnItemPlaced(Entity<ItemPlacerComponent> ent, ref ItemPlacedEvent args)
    {
        if (_heaters.TryGetValue(ent, out var state) && state.On)
            SpawnOnItem(args.OtherEntity, state);
    }

    private void OnItemRemoved(Entity<ItemPlacerComponent> ent, ref ItemRemovedEvent args)
    {
        if (_heaters.TryGetValue(ent, out var state) && state.Emitters.Remove(args.OtherEntity, out var emitter))
            _particles.RemoveParticle(emitter);
    }

    private void OnShutdown(Entity<EntityHeaterComponent> ent, ref ComponentShutdown args)
    {
        if (_heaters.Remove(ent, out var state))
            StopAll(state);

        if (_fire.Remove(ent, out var fire))
            _particles.RemoveParticle(fire);

        if (_smoke.Remove(ent, out var smoke))
            _particles.RemoveParticle(smoke);
    }

    private void SpawnOnItem(EntityUid item, HeaterState state)
    {
        if (state.Emitters.TryGetValue(item, out var emitter) && !emitter.Exhausted)
            return;

        state.Emitters.Remove(item);

        var coords = _transform.GetMapCoordinates(item);
        emitter = _particles.SpawnEffect(FireEffect, coords, item);
        if (emitter != null)
            state.Emitters[item] = emitter;
    }

    private void StopAll(HeaterState state)
    {
        foreach (var emitter in state.Emitters.Values)
            _particles.RemoveParticle(emitter);

        state.Emitters.Clear();
    }
}