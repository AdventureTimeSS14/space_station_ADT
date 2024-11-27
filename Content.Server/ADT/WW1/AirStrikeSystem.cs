using Content.Server.Explosion.EntitySystems;
using Content.Shared.ADT.WW1;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Coordinates;

namespace Content.Server.ADT.WW1;

public sealed class AirStrikeSystem : SharedAirStrikeSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirStrikeComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, AirStrikeComponent component, UseInHandEvent args)
    {
        if (component.IsArmed)
        {
            _popup.PopupEntity("Артиллерийный удар уже активирован!", args.User);
            return;
        }

        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
        {
            _popup.PopupEntity("Невозможно определить местоположение!", args.User);
            return;
        }

        _popup.PopupEntity("Артиллерийный удар активирован!", args.User, PopupType.Large);
        component.FireTime = _gameTiming.CurTime + component.FireDelay;
        component.IsArmed = true;
        component.StrikeOrigin = _transformSystem.GetMapCoordinates(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<AirStrikeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (EntityManager.Deleted(uid))
                continue;

            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
                return;

            component.StrikeCoordinates = transform.Coordinates;

            if (component.WarnSound != null && !component.WarnSoundPlayed && _gameTiming.CurTime >= component.FireTime - component.FireDelay / component.WarnSoundDelayMultiplier)
            {
                _audio.PlayPvs(component.WarnSound, component.StrikeCoordinates);
                _popup.PopupCoordinates("К вам приближается снаряд!", component.StrikeCoordinates, PopupType.LargeCaution);
                component.WarnSoundPlayed = true;
            }
            if (!component.IsArmed || _gameTiming.CurTime < component.FireTime)
                continue;

            TriggerAirStrike(uid, component);
        }
    }

    private void TriggerAirStrike(EntityUid uid, AirStrikeComponent component)
    {

        for (int i = 0; i < component.ExplosionCount; i++)
        {
            var offset = _random.NextFloat(component.MinOffset, component.MaxOffset);
            var angle = Angle.FromDegrees(_random.Next(0, 360));
            var explosionPoint = component.StrikeOrigin.Offset(angle.ToVec() * offset);

            var intensity = _random.NextFloat(component.MinExplosionIntensity, component.MaxExplosionIntensity);

            _explosionSystem.QueueExplosion(explosionPoint, "Default", intensity, 1.0f, intensity * 0.8f, null, 0f, 0);
            RemCompDeferred<AirStrikeComponent>(uid);
        }
    }
}
