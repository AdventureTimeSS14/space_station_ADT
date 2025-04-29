using Content.Shared.ADT.Supermatter.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Supermatter.Processing.Systems;

public sealed partial class SupermatterAnomaliesSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    /// <summary>
    /// Generate temporary anomalies depending on accumulated power.
    /// </summary>
    public void GenerateAnomalies(EntityUid uid, SupermatterComponent sm)
    {
        var xform = Transform(uid);
        var anomalies = new List<string>();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        // Bluespace anomaly
        if (_random.Prob(1 / sm.AnomalyBluespaceChance))
            anomalies.Add(sm.AnomalyBluespaceSpawnPrototype);

        // Gravity anomaly
        if (sm.Power > _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyGravityChanceSevere) ||
            _random.Prob(1 / sm.AnomalyGravityChance))
            anomalies.Add(sm.AnomalyGravitySpawnPrototype);

        // Pyroclastic anomaly
        if (sm.Power > _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyPyroChanceSevere) ||
            sm.Power > _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyPyroChance))
            anomalies.Add(sm.AnomalyPyroSpawnPrototype);

        var count = anomalies.Count;
        if (count == 0)
            return;

        var tiles = GetSpawningPoints(uid, sm, count);
        if (tiles == null)
            return;

        foreach (var tileref in tiles)
        {
            var anomaly = Spawn(_random.Pick(anomalies), _map.ToCenterCoordinates(tileref, grid));
            EnsureComp<TimedDespawnComponent>(anomaly).Lifetime = sm.AnomalyLifetime;
        }
    }

    /// <summary>
    /// Gets random points around the supermatter.
    /// </summary>
    public List<TileRef>? GetSpawningPoints(EntityUid uid, SupermatterComponent sm, int amount)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var localpos = xform.Coordinates.Position;
        var tilerefs = _map.GetLocalTilesIntersecting(
            xform.GridUid.Value,
            grid,
            new Box2(localpos + new Vector2(-sm.AnomalySpawnMaxRange, -sm.AnomalySpawnMaxRange), 
                   localpos + new Vector2(sm.AnomalySpawnMaxRange, sm.AnomalySpawnMaxRange)))
            .ToList();

        if (tilerefs.Count == 0)
            return null;

        var physQuery = GetEntityQuery<PhysicsComponent>();
        var resultList = new List<TileRef>();
        
        while (resultList.Count < amount)
        {
            if (tilerefs.Count == 0)
                break;

            var tileref = _random.Pick(tilerefs);
            var distance = MathF.Sqrt(
                MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + 
                MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2));

            if (distance > sm.AnomalySpawnMaxRange || distance < sm.AnomalySpawnMinRange)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            var valid = true;
            foreach (var ent in _map.GetAnchoredEntities(xform.GridUid.Value, grid, tileref.GridIndices))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;

                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int)CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }

            if (!valid)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            resultList.Add(tileref);
        }

        return resultList;
    }
}