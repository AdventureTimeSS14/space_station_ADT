using System.Numerics;
using Content.Client.ADT.Particles;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.ADT.Particles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly Vector2 EffectLift = new(0f, 0.08f);

    private readonly Dictionary<EntityUid, CookwareEmitters> _emitters = new();
    private readonly Dictionary<EntityUid, EntityUid> _ovenLoops = new();

    private ADTCookingSettingsPrototype Settings => _proto.Index<ADTCookingSettingsPrototype>(ADTCookingSettingsPrototype.DefaultId);

    private sealed class CookwareEmitters
    {
        public ADTCookwareEffect Effect;
        public EntityUid Host;
        public EntityUid? Loop;
        public readonly List<ActiveEmitter> Active = new();
    }

    private void InitializeParticles()
    {
        SubscribeLocalEvent<ParticleSettingsChangedEvent>(OnParticleSettingsChanged);
    }

    private void OnParticleSettingsChanged(ParticleSettingsChangedEvent ev)
    {
        foreach (var (uid, emitters) in _emitters)
        {
            StopEmitters(emitters);
            emitters.Effect = ADTCookwareEffect.None;
            if (TryComp<ADTCookwareComponent>(uid, out var comp))
                UpdateParticles((uid, comp));
        }
    }

    private void UpdateParticles(Entity<ADTCookwareComponent> ent)
    {
        _appearance.TryGetData<ADTCookwareEffect>(ent, ADTCookwareVisuals.Effect, out var effect);

        EntityUid host = ent;
        var offset = EffectLift;
        if (TryGetStoveSlot(ent, out var stove, out var slot))
        {
            host = stove;
            var stoveSlot = stove.Comp.Slots[slot];
            offset += stoveSlot.Offset;
            if (stoveSlot.Scale == null)
            {
                var lift = ent.Comp.StoveOffset;
                if (stoveSlot.Mirror)
                    lift.X = -lift.X;

                offset += lift;
            }
        }

        _emitters.TryGetValue(ent, out var emitters);
        if (emitters != null && emitters.Effect == effect && emitters.Host == host && !AnyExhausted(emitters))
            return;

        if (emitters != null)
            StopEmitters(emitters);

        if (effect == ADTCookwareEffect.None)
        {
            _emitters.Remove(ent);
            return;
        }

        emitters ??= new CookwareEmitters();
        emitters.Effect = effect;
        emitters.Host = host;
        _emitters[ent] = emitters;

        if (EffectLoop(effect) is { } loop)
            emitters.Loop = _audio.PlayEntity(loop, Filter.Local(), host, false, loop.Params.WithLoop(true))?.Entity;

        foreach (var proto in EffectParticles(effect))
        {
            var emitter = _particles.SpawnEffect(proto, _transform.GetMapCoordinates(host), host);
            if (emitter == null)
                continue;

            emitter.SpawnOffset = offset;
            emitters.Active.Add(emitter);
        }
    }

    private void RemoveParticles(EntityUid uid)
    {
        if (_emitters.Remove(uid, out var emitters))
            StopEmitters(emitters);
    }

    private void StopEmitters(CookwareEmitters emitters)
    {
        foreach (var emitter in emitters.Active)
            _particles.RemoveParticle(emitter);

        emitters.Active.Clear();
        emitters.Loop = _audio.Stop(emitters.Loop);
    }

    private void UpdateOvenLoop(Entity<ADTStoveComponent> ent)
    {
        var on = false;
        foreach (var slot in ent.Comp.Slots)
        {
            if (slot.Type == ADTStoveSlotType.Oven && slot.Heat != ADTStoveHeat.Off)
                on = true;
        }

        var playing = _ovenLoops.TryGetValue(ent, out var stream) && Exists(stream);
        if (on == playing)
            return;

        if (!on)
        {
            _audio.Stop(stream);
            _ovenLoops.Remove(ent);
            return;
        }

        if (Settings.OvenLoopSound is not { } sound)
            return;

        if (_audio.PlayEntity(sound, Filter.Local(), ent, false, sound.Params.WithLoop(true)) is { } played)
            _ovenLoops[ent] = played.Entity;
    }

    private void RemoveOvenLoop(EntityUid uid)
    {
        if (_ovenLoops.Remove(uid, out var stream))
            _audio.Stop(stream);
    }

    private SoundSpecifier? EffectLoop(ADTCookwareEffect effect)
    {
        var settings = Settings;
        return effect switch
        {
            ADTCookwareEffect.Sizzle => settings.FryLoopSound,
            ADTCookwareEffect.Steam => settings.BoilLoopSound,
            ADTCookwareEffect.Fire => settings.FireLoopSound,
            _ => null,
        };
    }

    private static bool AnyExhausted(CookwareEmitters emitters)
    {
        foreach (var emitter in emitters.Active)
        {
            if (emitter.Exhausted)
                return true;
        }

        return false;
    }

    private static IEnumerable<ProtoId<ParticleEffectPrototype>> EffectParticles(ADTCookwareEffect effect)
    {
        switch (effect)
        {
            case ADTCookwareEffect.Steam:
                yield return "ADTCookingSteam";
                break;
            case ADTCookwareEffect.Sizzle:
                yield return "ADTCookingSizzle";
                break;
            case ADTCookwareEffect.Smoke:
                yield return "ADTCookingSmoke";
                break;
            case ADTCookwareEffect.Fire:
                yield return "ADTCookingFire";
                yield return "ADTCookingSmoke";
                break;
        }
    }
}
