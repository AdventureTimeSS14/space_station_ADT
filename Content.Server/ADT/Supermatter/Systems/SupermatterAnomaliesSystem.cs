using System.Linq;
using Content.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Supermatter.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using System.Numerics;
using System;

namespace Content.Server.ADT.Supermatter.Systems;

public sealed partial class SupermatterSystem
{
    public AnomalyMode ChooseAnomalyType(EntityUid uid, SupermatterComponent sm)
    {
        if (sm.ResonantFrequency >= 1)
            return AnomalyMode.BeforeCascade;

        if (sm.Cascade == true)
            return AnomalyMode.AfterCascade;

        return AnomalyMode.Base;
    }

    /// <summary>
    /// Generate temporary anomalies depending on accumulated power.
    /// </summary>
    public void GenerateAnomalies(EntityUid uid, SupermatterComponent sm)
    {
        if (!sm.HasBeenPowered)
            return;
        
        sm.PreferredAnomalyMode = ChooseAnomalyType(uid, sm);
        
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var anomalies = new List<string>();

        switch (sm.PreferredAnomalyMode)
        {
            case AnomalyMode.BeforeCascade:
                if (_random.Prob(1 / sm.HalfLifePortalChance))
                    anomalies.Add(sm.HalfLifePortalPrototype);
                break;

            case AnomalyMode.AfterCascade:
                if (!sm.HasSpawnedPortal)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        anomalies.Add(sm.CascadePortalPrototype);
                    }
                    sm.HasSpawnedPortal = true;
                }
                break;

            case AnomalyMode.Base:
                if (_random.Prob(1 / sm.AnomalyBluespaceChance))
                    anomalies.Add(sm.AnomalyBluespaceSpawnPrototype);

                if (sm.Power > _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyGravityChanceSevere) ||
                    _random.Prob(1 / sm.AnomalyGravityChance))
                    anomalies.Add(sm.AnomalyGravitySpawnPrototype);

                if (sm.Power > _config.GetCVar(ADTCCVars.SupermatterSeverePowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyPyroChanceSevere) ||
                    sm.Power > _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold) && _random.Prob(1 / sm.AnomalyPyroChance))
                    anomalies.Add(sm.AnomalyPyroSpawnPrototype);
                break;
        }

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

        float maxRange = sm.PreferredAnomalyMode switch
        {
            AnomalyMode.BeforeCascade => sm.PortalSpawnMaxRange,
            AnomalyMode.AfterCascade => sm.PortalSpawnMaxRange,
            _ => sm.AnomalySpawnMaxRange
        };

        var localpos = xform.Coordinates.Position;
        var tilerefs = _map.GetLocalTilesIntersecting(
            xform.GridUid.Value,
            grid,
            new Box2(localpos + new Vector2(-maxRange, -maxRange),
                    localpos + new Vector2(maxRange, maxRange)))
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

            if (distance > maxRange || distance < sm.AnomalySpawnMinRange)
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