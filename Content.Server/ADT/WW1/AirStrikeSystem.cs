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
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.ADT.WW1;

public sealed class AirStrikeSystem : SharedAirStrikeSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private EntityCoordinates localCoordinates;
    private readonly ISawmill _sawmill = Logger.GetSawmill("airstrike");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AirStrikeComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, AirStrikeComponent component, UseInHandEvent args)
    {
        if (component.IsArmed)
        {
            _popup.PopupEntity(Loc.GetString("airstrike-already-activated"), args.User);
            return;
        }

        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
        {
            _popup.PopupEntity(Loc.GetString("airstrike-unlocatable"), args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString("airstrike-activated"), args.User, PopupType.Large);
        _adminLogger.Add(LogType.AdminMessage, LogImpact.High, $"{ToPrettyString(args.User)} caused an artillery strike of {ToPrettyString(uid)}");
        component.FireTime = _gameTiming.CurTime + component.FireDelay;
        component.IsArmed = true;
        component.StrikeOrigin = _transformSystem.GetMapCoordinates(uid);
        localCoordinates = transform.Coordinates;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<AirStrikeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (EntityManager.Deleted(uid))
                continue;


            if (component.WarnSound != null && !component.WarnSoundPlayed && _gameTiming.CurTime >= component.FireTime - component.FireDelay / component.WarnSoundDelayMultiplier)
            {
                _audio.PlayPvs(component.WarnSound, localCoordinates);
                _popup.PopupCoordinates(Loc.GetString("airstrike-shell-coming"), localCoordinates, PopupType.LargeCaution);
                component.WarnSoundPlayed = true;
            }
            if (!component.IsArmed || _gameTiming.CurTime < component.FireTime)
                continue;

            TriggerAirStrike(uid, component);
        }
    }

    private void TriggerAirStrike(EntityUid uid, AirStrikeComponent component)
    {
        if (component.ExplosiveType == null)
        {
            _sawmill.Warning("ExplosiveType is not set for AirStrikeComponent");
            component.ExplosiveType = new ExplosiveTypeData
            {
                Type = ExplosiveType.Explosive
            };
        }

        for (int i = 0; i < component.ExplosionCount; i++)
        {
            var offset = _random.NextFloat(component.MinOffset, component.MaxOffset);
            var angle = Angle.FromDegrees(_random.Next(0, 360));
            var explosionPoint = component.StrikeOrigin.Offset(angle.ToVec() * offset);

            var intensity = _random.NextFloat(component.MinExplosionIntensity, component.MaxExplosionIntensity);

            switch (component.ExplosiveType.Type)
            {
                case ExplosiveType.Explosive:
                    _explosionSystem.QueueExplosion(
                        explosionPoint,
                        "Default",
                        intensity,
                        1.0f,
                        intensity * 0.8f,
                        null,
                        0f,
                        0
                    );
                    break;

                case ExplosiveType.Smoke:
                    if (component.ExplosiveType.PrototypeId == null)
                    {
                        _sawmill.Warning($"PrototypeId is missing for ExplosiveType {component.ExplosiveType.Type}.");
                        continue;
                    }

                    Spawn(component.ExplosiveType.PrototypeId.Value, explosionPoint);
                    break;

                default:
                    _sawmill.Warning($"Unknown ExplosiveType {component.ExplosiveType.Type} in AirStrikeComponent.");
                    break;
            }
        }
        RemCompDeferred<AirStrikeComponent>(uid);
    }
}
