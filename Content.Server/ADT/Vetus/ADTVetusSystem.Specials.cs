using Content.Shared.ADT.Vetus;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private void DoSpecialMove(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        _megafauna.Say(ent, "adt-vetus-special", 4, true);

        switch (ent.Comp.Mode)
        {
            case ADTVetusMode.Bluespace:
                BluespaceSlowdown(ent, target);
                break;
            case ADTVetusMode.Grav:
                RockThrow(ent, target);
                break;
            case ADTVetusMode.Pyro:
                Volcanoes(ent, target);
                break;
            case ADTVetusMode.Flux:
                TeslaShots(ent);
                break;
            case ADTVetusMode.Vortex:
                Earthquake(ent, target);
                break;
        }
    }

    private void BluespaceSlowdown(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        _popup.PopupEntity(Loc.GetString("adt-vetus-bluespace-slowdown", ("vetus", ent.Owner)), target, target, PopupType.LargeCaution);
        _statusEffects.TryAddStatusEffectDuration(target, ent.Comp.BluespaceSlowdown, ent.Comp.BluespaceSlowdownDuration);
    }

    private void RockThrow(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("adt-vetus-rocks"), ent, PopupType.LargeCaution);

        var spots = GetFreeTilesAround(_transform.GetMapCoordinates(target), ent.Comp.SpecialRange, ent.Comp.RockMinRange, true);

        for (var i = 0; i < ent.Comp.SpecialCount && spots.Count > 0; i++)
        {
            var spot = _random.PickAndTake(spots);
            var rock = Spawn(ent.Comp.FloatingRock, spot);
            EnsureComp<TimedDespawnComponent>(rock).Lifetime = (float) ent.Comp.RockDelay.TotalSeconds;

            _megafauna.Schedule(ent, ent.Comp.RockDelay, () =>
            {
                if (TerminatingOrDeleted(target))
                    return;

                ShootProjectile(ent, ent.Comp.RockProjectile, spot, _transform.GetMapCoordinates(target), ent.Comp.ProjectileSpeed);
            });
        }
    }

    private void Volcanoes(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("adt-vetus-volcanoes"), ent, PopupType.LargeCaution);

        var spots = GetFreeTilesAround(_transform.GetMapCoordinates(target), ent.Comp.SpecialRange, ent.Comp.VolcanoMinRange, true);

        for (var i = 0; i < ent.Comp.SpecialCount && spots.Count > 0; i++)
        {
            var spot = _random.PickAndTake(spots);

            if (!_map.TryFindGridAt(spot, out var gridUid, out var grid))
                continue;

            var center = _map.TileIndicesFor(gridUid, grid, spot);

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var tile = center + new Vector2i(x, y);

                    if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
                        continue;

                    _drakeEffects.SpawnLavaWarning(ent.Comp.LavaWarning, gridUid, grid, tile, ent.Comp.LavaResetTime);
                }
            }
        }
    }

    private void TeslaShots(Entity<ADTVetusComponent> ent)
    {
        var origin = _transform.GetMapCoordinates(ent);

        _humanoids.Clear();
        _lookup.GetEntitiesInRange(origin, ent.Comp.FluxRange * MathF.Sqrt(2f) + 0.5f, _humanoids);

        foreach (var humanoid in _humanoids)
        {
            if (GetTileDistance(ent, humanoid) > ent.Comp.FluxRange)
                continue;

            if (!_examine.InRangeUnOccluded(ent, humanoid, ent.Comp.FluxRange * MathF.Sqrt(2f) + 0.5f))
                continue;

            ShootProjectile(ent, ent.Comp.FluxProjectile, origin, _transform.GetMapCoordinates(humanoid), ent.Comp.ProjectileSpeed);
        }
    }

    private void Earthquake(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("adt-vetus-earthquake", ("vetus", ent.Owner)), ent, PopupType.LargeCaution);

        var center = _transform.GetMapCoordinates(target);

        if (!_map.TryFindGridAt(center, out var gridUid, out var grid))
            return;

        var origin = _map.TileIndicesFor(gridUid, grid, center);
        var range = ent.Comp.VortexRange;

        for (var x = -range; x <= range; x++)
        {
            for (var y = -range; y <= range; y++)
            {
                if (!_random.Prob(ent.Comp.VortexChance))
                    continue;

                var tile = origin + new Vector2i(x, y);

                if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
                    continue;

                Spawn(ent.Comp.VortexTarget, _map.GridTileToLocal(gridUid, grid, tile));
            }
        }
    }

    private void SpawnAnomalies(Entity<ADTVetusComponent> ent)
    {
        if (ent.Comp.Mode is not { } mode || !ent.Comp.Anomalies.TryGetValue(mode, out var proto))
            return;

        _megafauna.Say(ent, "adt-vetus-anomalies", 3, true);

        var spots = GetFreeTilesAround(_transform.GetMapCoordinates(ent), ent.Comp.AnomalyRange, -1, true);

        for (var i = 0; i < ent.Comp.AnomalyCount && spots.Count > 0; i++)
        {
            var anomaly = Spawn(proto, _random.PickAndTake(spots));
            EnsureComp<TimedDespawnComponent>(anomaly).Lifetime = (float) ent.Comp.AnomalyLifetime.TotalSeconds;
        }
    }
}
