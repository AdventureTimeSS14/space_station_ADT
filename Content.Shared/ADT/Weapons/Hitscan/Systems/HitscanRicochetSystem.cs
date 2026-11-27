using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Combat.Ranged.Pierce;
using Content.Shared.ADT.Weapons.Hitscan.Components;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Weapons.Hitscan.Systems;

/// <summary>
/// On attempt (before damage), may cancel the hit and bounce the hitscan trace.
/// </summary>
public sealed class HitscanRicochetSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<HitscanReflectComponent> _reflectQuery;

    public override void Initialize()
    {
        base.Initialize();

        _reflectQuery = GetEntityQuery<HitscanReflectComponent>();

        SubscribeLocalEvent<HitscanRicochetComponent, AttemptHitscanRaycastFiredEvent>(OnHitscanHit);
        SubscribeLocalEvent<RicochetableComponent, HitScanRicochetAttemptEvent>(OnRicochet);
    }

    private void OnHitscanHit(Entity<HitscanRicochetComponent> hitscan, ref AttemptHitscanRaycastFiredEvent args)
    {
        var data = args.Data;

        if (hitscan.Comp.Chance <= 0 || data.HitEntity == null || data.HitPosition == null)
            return;

        if (!_reflectQuery.TryComp(hitscan.Owner, out var reflect) || reflect.CurrentReflections > reflect.MaxReflections)
            return;

        var ev = new HitScanRicochetAttemptEvent(hitscan.Comp.Chance, data.HitPosition.Value, data.ShotDirection, false);
        RaiseLocalEvent(data.HitEntity.Value, ref ev);

        if (!ev.Ricocheted)
            return;

        reflect.CurrentReflections++;
        args.Cancelled = true;

        var fromEffect = Transform(data.HitEntity.Value).Coordinates;
        if (Transform(data.HitEntity.Value).MapUid is { } hitMap)
            fromEffect = new EntityCoordinates(hitMap, data.HitPosition.Value);

        var hitFiredEvent = new HitscanTraceEvent
        {
            FromCoordinates = fromEffect,
            ToCoordinates = fromEffect.Offset(ev.Dir), // ADT hitscan #3142
            ShotDirection = ev.Dir,
            Gun = data.Gun,
            Shooter = data.Shooter, // keep original shooter ignored
            IgnoredEntity = data.HitEntity, // don't immediately re-hit bounce surface
            OutputTrace = data.OutputTrace,
        };

        RaiseLocalEvent(hitscan, ref hitFiredEvent);
    }

    private void OnRicochet(Entity<RicochetableComponent> ent, ref HitScanRicochetAttemptEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || fixtures.Fixtures.Count == 0
            || fixtures.Fixtures.FirstOrDefault().Value?.Shape is not PolygonShape shape)
            return;

        var chance = Math.Clamp(args.Chance * ent.Comp.Chance, 0f, 1f);
        if (chance == 0)
            return;

        var invMatrix = _transform.GetInvWorldMatrix(ent.Owner);
        var localFrom = Vector2.Transform(args.Pos, invMatrix);

        var invNoTrans = invMatrix;
        invNoTrans.M31 = 0f;
        invNoTrans.M32 = 0f;

        var localDir = Vector2.Transform(args.Dir, invNoTrans).Normalized();

        if (!RayCastPolygon(shape, localFrom, localDir, out _, out var edgeIndex, out _))
            return;

        var localNormal = shape.Normals[edgeIndex];
        var dot = Vector2.Dot(localDir, localNormal);
        var clampedDot = Math.Clamp(MathF.Abs(dot), 0f, 1f);
        var angleFactor = 2f * (1f - clampedDot);

        chance = Math.Clamp(args.Chance * angleFactor, 0f, 1f);
        if (!_rand.Prob(chance))
            return;

        var reflectedLocal = localDir - 2f * dot * localNormal;

        var matrix = _transform.GetWorldMatrix(ent.Owner);
        var matrixNoTrans = matrix;
        matrixNoTrans.M31 = 0f;
        matrixNoTrans.M32 = 0f;

        args.Dir = Vector2.Transform(reflectedLocal, matrixNoTrans).Normalized();
        args.Ricocheted = true;
    }

    private static bool RayCastPolygon(
        PolygonShape polygon,
        Vector2 origin,
        Vector2 dir,
        out float tMin,
        out int edgeIndex,
        out Vector2 ptLocal,
        float maxT = float.MaxValue)
    {
        tMin = float.MaxValue;
        edgeIndex = -1;
        ptLocal = default;

        var verts = polygon.Vertices;
        var count = polygon.VertexCount;

        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            if (RayCastSegment(origin, dir, verts[i], verts[next], out var t) && t >= 0f && t < maxT && t < tMin)
            {
                tMin = t;
                edgeIndex = i;
            }
        }

        if (edgeIndex < 0)
            return false;

        ptLocal = origin + dir * tMin;
        return true;
    }

    private static bool RayCastSegment(Vector2 origin, Vector2 dir, Vector2 v0, Vector2 v1, out float t)
    {
        t = 0f;
        var edge = v1 - v0;
        var denom = Cross2D(edge, dir);
        if (MathF.Abs(denom) < 1e-6f)
            return false;

        var diff = origin - v0;
        var s = Cross2D(diff, dir) / denom;
        if (s is < 0f or > 1f)
            return false;

        var tRay = Cross2D(diff, edge) / denom;
        if (tRay < 0f)
            return false;

        t = tRay;
        return true;
    }

    private static float Cross2D(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
