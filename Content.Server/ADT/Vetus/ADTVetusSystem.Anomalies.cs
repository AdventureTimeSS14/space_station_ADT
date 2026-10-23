using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Throwing;
using Content.Shared.ADT.Vetus;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Explosion;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Wall;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    private static readonly TimeSpan AnomalyShockTime = TimeSpan.FromSeconds(4);

    private const float WallLightBreakChance = 0.4f;
    private const float GravItemThrowRange = 5f;
    private const float GravItemThrowSpeed = 50f;
    private const int GravItemTargetRange = 4;

    private readonly HashSet<EntityUid> _anomalyArea = new();

    private void InitializeAnomalies()
    {
        SubscribeLocalEvent<ADTVetusAnomalyComponent, MapInitEvent>(OnAnomalyMapInit);
        SubscribeLocalEvent<ADTVetusAnomalyComponent, StepTriggerAttemptEvent>(OnAnomalyStepAttempt);
        SubscribeLocalEvent<ADTVetusAnomalyComponent, StepTriggeredOffEvent>(OnAnomalyEntered);
        SubscribeLocalEvent<ADTVetusAnomalyComponent, StartCollideEvent>(OnAnomalyBump);
        SubscribeLocalEvent<ADTVetusAnomalyComponent, BeforeExplodeEvent>(OnAnomalyBeforeExplode);
        SubscribeLocalEvent<ADTVetusAnomalyComponent, TimedDespawnEvent>(OnAnomalyDespawn);
    }

    private void OnAnomalyMapInit(Entity<ADTVetusAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextProcess = _timing.CurTime + ent.Comp.ProcessInterval;
    }

    private void OnAnomalyStepAttempt(Entity<ADTVetusAnomalyComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnAnomalyEntered(Entity<ADTVetusAnomalyComponent> ent, ref StepTriggeredOffEvent args)
    {
        switch (ent.Comp.Mode)
        {
            case ADTVetusMode.Grav:
                GravShock(ent, args.Tripper);
                break;
            case ADTVetusMode.Flux:
                AnomalyShock(ent, args.Tripper);
                break;
        }
    }

    private void OnAnomalyBump(Entity<ADTVetusAnomalyComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        switch (ent.Comp.Mode)
        {
            case ADTVetusMode.Grav:
                GravShock(ent, other);
                break;
            case ADTVetusMode.Flux:
                AnomalyShock(ent, other);
                break;
            case ADTVetusMode.Bluespace:
                if (HasComp<MobStateComponent>(other))
                    TeleportAround(other, ent.Comp.BumpTeleportRange);
                break;
        }
    }

    private void OnAnomalyBeforeExplode(Entity<ADTVetusAnomalyComponent> ent, ref BeforeExplodeEvent args)
    {
        if (GetExplosionSeverity(ent, ref args, 4f, 8f, false) >= 3)
            QueueDel(ent);
    }

    private void OnAnomalyDespawn(Entity<ADTVetusAnomalyComponent> ent, ref TimedDespawnEvent args)
    {
        if (ent.Comp.Mode == ADTVetusMode.Flux)
            Spawn("EffectSparks", _transform.GetMapCoordinates(ent));
    }

    private void UpdateAnomalies(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTVetusAnomalyComponent>();

        while (query.MoveNext(out var uid, out var anomaly))
        {
            if (now < anomaly.NextProcess)
                continue;

            anomaly.NextProcess = now + anomaly.ProcessInterval;
            ProcessAnomaly((uid, anomaly));
        }
    }

    private void ProcessAnomaly(Entity<ADTVetusAnomalyComponent> ent)
    {
        if (_random.Prob(ent.Comp.MoveChance))
            StepAnomaly(ent);

        if (!TryGetTile(ent, out var gridUid, out var grid, out var tile))
            return;

        switch (ent.Comp.Mode)
        {
            case ADTVetusMode.Grav:
                ent.Comp.Charged = true;
                GravPulse(ent, gridUid, grid, tile);
                break;
            case ADTVetusMode.Flux:
                ent.Comp.Charged = true;

                foreach (var mob in GetMobsOnTile(gridUid, grid, tile))
                {
                    AnomalyShock(ent, mob);
                }

                break;
            case ADTVetusMode.Bluespace:
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        foreach (var mob in GetMobsOnTile(gridUid, grid, tile + new Vector2i(x, y)))
                        {
                            TeleportAround(mob, ent.Comp.ProcessTeleportRange);
                        }
                    }
                }

                break;
            case ADTVetusMode.Pyro:
                ent.Comp.Ticks++;

                if (ent.Comp.Ticks < ent.Comp.GasTicks)
                    break;

                ent.Comp.Ticks = 0;
                ReleaseHotGas(ent);
                break;
            case ADTVetusMode.Vortex:
                VortexPulse(ent, gridUid, grid, tile);
                break;
        }
    }

    private void StepAnomaly(Entity<ADTVetusAnomalyComponent> ent)
    {
        if (!TryGetTile(ent, out var gridUid, out var grid, out var tile))
            return;

        var target = tile + _random.Pick(AllOffsets);

        if (!IsTileFree(gridUid, grid, target))
            return;

        _transform.SetCoordinates(ent, _map.GridTileToLocal(gridUid, grid, target));
    }

    private List<EntityUid> GetMobsOnTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var result = new List<EntityUid>();

        foreach (var entity in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid))
        {
            if (HasComp<MobStateComponent>(entity))
                result.Add(entity);
        }

        return result;
    }

    private void GravPulse(Entity<ADTVetusAnomalyComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var range = ent.Comp.PullRange;
        var center = _transform.GetMapCoordinates(ent);

        _anomalyArea.Clear();
        _lookup.GetEntitiesInRange(center.MapId, center.Position, range * MathF.Sqrt(2f) + 0.5f, _anomalyArea);

        foreach (var other in _anomalyArea.ToList())
        {
            if (other == ent.Owner || TerminatingOrDeleted(other) || GetTileDistance(ent, other) > range)
                continue;

            if (HasComp<MobStateComponent>(other))
            {
                if (!HasComp<ADTThrowImmuneComponent>(other))
                    StepTowards(other, ent);

                continue;
            }

            if (!Transform(other).Anchored && !HasComp<ADTVetusAnomalyComponent>(other))
                StepTowards(other, ent);
        }

        foreach (var mob in GetMobsOnTile(gridUid, grid, tile))
        {
            GravShock(ent, mob);
        }

        foreach (var other in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid).ToList())
        {
            if (other == ent.Owner || HasComp<MobStateComponent>(other) || Transform(other).Anchored)
                continue;

            if (HasComp<ADTThrowImmuneComponent>(other) || HasComp<ADTVetusAnomalyComponent>(other))
                continue;

            if (FindConsciousMobInView(ent, GravItemTargetRange) is not { } target)
                continue;

            var direction = _transform.GetMapCoordinates(target).Position - center.Position;

            if (direction.LengthSquared() < 0.01f)
                continue;

            _throwing.TryThrow(other, Vector2.Normalize(direction) * GravItemThrowRange, GravItemThrowSpeed, recoil: false);
        }
    }

    private EntityUid? FindConsciousMobInView(EntityUid source, int range)
    {
        _legMobs.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(source), range * MathF.Sqrt(2f) + 0.5f, _legMobs);

        foreach (var mob in _legMobs)
        {
            if (!_mobState.IsAlive(mob) || GetTileDistance(source, mob) > range)
                continue;

            if (!_examine.InRangeUnOccluded(source, mob, range * MathF.Sqrt(2f) + 0.5f))
                continue;

            return mob;
        }

        return null;
    }

    private void GravShock(Entity<ADTVetusAnomalyComponent> ent, EntityUid mob)
    {
        if (!ent.Comp.Charged || !HasComp<MobStateComponent>(mob) || !_mobState.IsAlive(mob))
            return;

        ent.Comp.Charged = false;

        var away = _transform.GetMapCoordinates(mob).Position - _transform.GetMapCoordinates(ent).Position;
        away = away.LengthSquared() < 0.01f ? _random.NextAngle().ToVec() : Vector2.Normalize(away);

        _throwing.TryThrow(mob, away * ent.Comp.GravThrowDistance, ent.Comp.GravThrowSpeed, recoil: false);
    }

    private void AnomalyShock(Entity<ADTVetusAnomalyComponent> ent, EntityUid mob)
    {
        if (!ent.Comp.Charged || !HasComp<MobStateComponent>(mob))
            return;

        ent.Comp.Charged = false;
        _electrocution.TryDoElectrocution(mob, ent, ent.Comp.ShockDamage, AnomalyShockTime, true, ignoreInsulation: true);
    }

    private void TeleportAround(EntityUid uid, int range)
    {
        var spots = GetFreeTilesAround(_transform.GetMapCoordinates(uid), range, -1, false);

        if (spots.Count == 0)
            return;

        _transform.SetMapCoordinates(uid, _random.Pick(spots));
    }

    private void ReleaseHotGas(Entity<ADTVetusAnomalyComponent> ent)
    {
        if (_atmos.GetTileMixture(ent.Owner, true) is not { } tileMixture)
            return;

        var gas = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = ent.Comp.GasTemperature,
        };

        gas.AdjustMoles(Gas.Plasma, ent.Comp.GasMoles);
        gas.AdjustMoles(Gas.Oxygen, ent.Comp.GasMoles);
        _atmos.Merge(tileMixture, gas);
    }

    private void VortexPulse(Entity<ADTVetusAnomalyComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var radius = _random.Next(0, ent.Comp.VortexMaxRadius + 1);
        var severity = _random.Next(0, 2) == 0 ? 2 : 1;

        for (var t = -radius; t < radius; t++)
        {
            VortexAffect(ent, gridUid, grid, tile + new Vector2i(t, -radius), severity);
            VortexAffect(ent, gridUid, grid, tile + new Vector2i(-t, radius), severity);
            VortexAffect(ent, gridUid, grid, tile + new Vector2i(radius, t), severity);
            VortexAffect(ent, gridUid, grid, tile + new Vector2i(-radius, -t), severity);
        }
    }

    private void VortexAffect(Entity<ADTVetusAnomalyComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile, int severity)
    {
        if (_random.Prob(ent.Comp.VortexPullChance))
        {
            foreach (var other in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid).ToList())
            {
                if (other == ent.Owner || TerminatingOrDeleted(other) || HasComp<ADTVetusAnomalyComponent>(other))
                    continue;

                if (HasComp<ADTMineralComponent>(other) || HasComp<WallComponent>(other))
                    continue;

                if (HasComp<MobStateComponent>(other) || !Transform(other).Anchored)
                {
                    StepTowards(other, ent);
                    continue;
                }

                ObjectExplosionAct(other, severity);
            }
        }

        if (!_random.Prob(ent.Comp.VortexTurfChance))
            return;

        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (TryComp<ADTMineralComponent>(anchored, out var mineral))
            {
                _mineral.ExplosionAct((anchored, mineral), severity);
                return;
            }

            if (!HasComp<WallComponent>(anchored))
                continue;

            if (severity >= 2 || _random.Prob(WallLightBreakChance))
                _destructible.DestroyEntity(anchored);

            return;
        }
    }

    private void ObjectExplosionAct(EntityUid uid, int severity)
    {
        if (!HasComp<DamageableComponent>(uid) || HasComp<ItemComponent>(uid))
            return;

        var amount = severity >= 2 ? _random.Next(100, 251) : _random.Next(10, 91);
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", amount);
        _damageable.TryChangeDamage(uid, damage, true);
    }

    private void StepTowards(EntityUid uid, EntityUid target)
    {
        if (!TryGetTile(uid, out var gridUid, out var grid, out var tile) || !TryGetTile(target, out var targetGrid, out _, out var targetTile))
            return;

        if (gridUid != targetGrid || tile == targetTile || Transform(uid).ParentUid != gridUid)
            return;

        var step = new Vector2i(Math.Sign(targetTile.X - tile.X), Math.Sign(targetTile.Y - tile.Y));

        if (!IsTileFree(gridUid, grid, tile + step))
            return;

        var xform = Transform(uid);
        _transform.SetLocalPosition(uid, xform.LocalPosition + new Vector2(step.X, step.Y), xform);
    }
}
