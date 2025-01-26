using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Anomaly.Effects.Components;
using Robust.Shared.Player;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.ADT.Silicon.Components;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Collections;
using System.Linq;

namespace Content.Server.ADT.Anomaly.Effects;
public sealed class StaticAnomalySystem : EntitySystem
{
    [Dependency] protected readonly IEntityManager EntMan = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        /*        SubscribeLocalEvent<StaticAnomalyComponent, AnomalyPulseEvent>(OnPulse);*/
        //  SubscribeLocalEvent<SeeingStaticComponent, LocalPlayerAttachedEvent>(_static.OnPlayerAttached);
        //    SubscribeLocalEvent<SeeingStaticComponent, LocalPlayerDetachedEvent>(_static.OnPlayerDetached);*/

        /*        SubscribeLocalEvent<StaticAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);*/
        //   SubscribeLocalEvent<StaticAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
    }

    /*    EntityCoordinates _coords;*/
    /*    public readonly EntityUid uid;*/
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaticAnomalyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            foreach (var ent in _lookup.GetEntitiesInRange(uid, comp.NoiseRange))
            {
                if (!_entityManager.TryGetComponent<SeeingStaticComponent>(ent, out var staticComp))
                    return;
                _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);

                staticComp.Multiplier = 0.55f;
            }
        }
    } // TODO: выяснить как SeeingStatic определяет кто робот, а кто нет. добавить более плавное вхождение и настройку силы
}





/*private void OnPulse(EntityUid uid, StaticAnomalyComponent component, ref AnomalyPulseEvent args)
{
    foreach (var ent in _lookup.GetEntitiesInRange(uid, 1f))
    {
        _status.TryAddStatusEffect<SeeingStaticComponent>(ent, "SeeingStatic", TimeSpan.FromSeconds(5f), true);
    }
}*/



    //Entity<StaticAnomalyComponent> anomaly
    /*    var player = _player.LocalSession;
        var uid = player?.AttachedEntity;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.NoiseRange, mobs);
        var allEnts = new ValueList<EntityUid>(mobs.Select(m => m.Owner)) { uid };
        var xform = Transform(uid);

        foreach (var ent in _lookup.GetEntitiesInRange(xform.Coordinates, component.NoiseRange))
        {
            _status.TryAddStatusEffect<SeeingStaticComponent>(ent, SharedSeeingStaticSystem.StaticKey, TimeSpan.FromSeconds(10f), true);
            statics.Multiplier = anomaly.Comp.NoiseRange * args.Severity;
        }*/
    /*        Transform(anomaly).Coordinates.TryDistance(EntityManager, _coords, out var distance);*/
    /*        if (distance <= anomaly.Comp.NoiseRange)*/
    /*        if ((uid != null) && (Transform(uid).Coordinates.TryDistance(EntityManager, Transform(anomaly).Coordinates, out var distance) &&
                distance <= anomaly.Comp.NoiseRange))
            {
                if (!TryComp<StatusEffectsComponent>(uid, out var statusComp))
                    return;

                if (TryComp<SeeingStaticComponent>(anomaly, out var statics))
                {

                    _status.TryAddStatusEffect<SeeingStaticComponent>(uid, SharedSeeingStaticSystem.StaticKey, TimeSpan.FromSeconds(10f), true, statusComp);
                    statics.Multiplier = anomaly.Comp.NoiseRange * args.Severity;
                }
            }
            else
            {
                if (TryComp<SeeingStaticComponent>(anomaly, out var statics))
                    statics.Multiplier = 0;
            }
        }*/
    /*    public void OnPlayerAttached(EntityUid uid, SeeingStaticComponent component, LocalPlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
        }*/
    /*        private void OnPulse(Entity<StaticAnomalyComponent> anomaly, ref AnomalyPulseEvent args, EntityUid uid)
        {
            var range = anomaly.Comp.MadnessRange;
            var newMessage = SanitizeMessageReplaceWords(message.Trim());

            int boltCount = (int)MathF.Floor(MathHelper.Lerp((float)anomaly.Comp.MinBoltCount, (float)anomaly.Comp.MaxBoltCount, args.Severity));

            _chat.TrySanitizeEmoteShorthands(newMessage, uid, "chatsan-laughs");
        }*/

