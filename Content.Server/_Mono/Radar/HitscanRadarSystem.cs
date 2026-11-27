using System.Numerics;
using Content.Shared._Mono.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Map;
using Robust.Shared.Spawners;

namespace Content.Server._Mono.Radar;

public sealed partial class HitscanRadarSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        // ADT-Tweak: the trace event has the origin, the fired event only knows what got hit
        SubscribeLocalEvent<HitscanRadarSignatureComponent, HitscanTraceEvent>(OnHitscanTrace);
        SubscribeLocalEvent<HitscanRadarSignatureComponent, HitscanRaycastFiredEvent>(OnHitscanRaycastFired);
    }

    private void OnHitscanTrace(Entity<HitscanRadarSignatureComponent> ent, ref HitscanTraceEvent ev)
    {
        if (!ent.Comp.Enabled)
            return;

        var shooter = ev.Shooter ?? ev.Gun; // If "there is no shooter" then the shooter is a gun
        var radarEntity = Spawn(null, new EntityCoordinates(shooter, Vector2.Zero));
        var radarComponent = EnsureComp<HitscanRadarComponent>(radarEntity);
        var maxDistance = CompOrNull<HitscanBasicRaycastComponent>(ent)?.MaxDistance ?? 0f;
        var startPos = _transform.ToMapCoordinates(ev.FromCoordinates).Position;

        radarComponent.RadarColor = ent.Comp.RadarColor;
        radarComponent.LineThickness = ent.Comp.LineThickness;
        radarComponent.LifeTime = ent.Comp.LifeTime;
        radarComponent.StartPosition = startPos;
        radarComponent.EndPosition = startPos + ev.ShotDirection.Normalized() * maxDistance;
        radarComponent.OriginGrid = Transform(shooter).GridUid;

        EnsureComp<TimedDespawnComponent>(radarEntity).Lifetime = radarComponent.LifeTime; // Make sure the radar entity gets cleaned up
        ent.Comp.RadarLine = radarEntity;
    }

    private void OnHitscanRaycastFired(Entity<HitscanRadarSignatureComponent> ent, ref HitscanRaycastFiredEvent ev)
    {
        if (ev.Data.HitEntity is not { } hit || !TryComp<HitscanRadarComponent>(ent.Comp.RadarLine, out var line))
            return;

        // Cut the line at the hit, projected onto the beam
        var dir = ev.Data.ShotDirection.Normalized();
        var along = Vector2.Dot(_transform.GetWorldPosition(hit) - line.StartPosition, dir);
        if (along > 0f)
            line.EndPosition = line.StartPosition + dir * along;
    }
}
