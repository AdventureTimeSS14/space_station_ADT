using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanReflectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanReflectComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanReflectComponent> hitscan, ref AttemptHitscanRaycastFiredEvent args)
    {
        var data = args.Data;

        if (hitscan.Comp.ReflectiveType == ReflectType.None || data.HitEntity == null)
            return;

        if (hitscan.Comp.CurrentReflections >= hitscan.Comp.MaxReflections)
            return;

        var ev = new HitScanReflectAttemptEvent(data.Shooter ?? data.Gun, data.Gun, hitscan.Comp.ReflectiveType, data.ShotDirection, false);
        RaiseLocalEvent(data.HitEntity.Value, ref ev);

        if (!ev.Reflected)
            return;

        hitscan.Comp.CurrentReflections++;

        args.Cancelled = true;

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;
        if (Transform(data.HitEntity.Value).MapUid is { } hitMap && data.HitPosition is { } hitPosition)
            fromEffect = new EntityCoordinates(hitMap, hitPosition);

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ToCoordinates = fromEffect.Offset(ev.Direction), // ADT hitscan #3142
            ShotDirection = ev.Direction,
            Gun = data.Gun,
            Shooter = data.Shooter, // keep original shooter ignored
            IgnoredEntity = data.HitEntity, // don't immediately re-hit reflector
            OutputTrace = data.OutputTrace,
        };

        RaiseLocalEvent(hitscan, ref hitFiredEvent);
    }
}
