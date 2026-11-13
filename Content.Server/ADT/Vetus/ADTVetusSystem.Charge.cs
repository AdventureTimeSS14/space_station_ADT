using System.Linq;
using System.Numerics;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Vetus;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    [Dependency] private ADTMegafaunaChargeSystem _charge = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private void InitializeCharge()
    {
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaChargeTelegraphEvent>(OnChargeTelegraph);
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaChargeTrampledEvent>(OnChargeTrampled);
        SubscribeLocalEvent<ADTVetusComponent, ADTMegafaunaChargeEndedEvent>(OnChargeEnded);
        SubscribeLocalEvent<ADTVetusComponent, MoveEvent>(OnVetusMove);
    }

    private ADTMegafaunaChargeOptions GetChargeOptions(ADTVetusComponent vetus)
    {
        return new ADTMegafaunaChargeOptions
        {
            Overshoot = 0f,
            MinDistance = 0f,
            LeadTarget = false,
            SmashPath = false,
            PassThroughMobs = true,
            TrampleRadius = vetus.ChargeTrampleRadius,
            ArmorPenetration = vetus.ChargeArmorPenetration,
            TelegraphLifetime = vetus.ChargeTelegraphLifetime,
        };
    }

    private void TripleCharge(Entity<ADTVetusComponent> ent, EntityUid target)
    {
        var delays = ent.Comp.Mode == ADTVetusMode.Bluespace
            ? ent.Comp.BluespaceChargeDelays
            : ent.Comp.ChargeDelays;

        if (delays.Count == 0)
            return;

        _charge.BeginChargeSeries(
            ent,
            _transform.GetMapCoordinates(target),
            target,
            delays.Select(d => (float) d.TotalSeconds).ToList(),
            0f,
            ent.Comp.ChargeSpeed,
            ent.Comp.ChargeTelegraph,
            ent.Comp.ChargeDamage,
            GetChargeOptions(ent.Comp),
            chained: true);
    }

    private void OnChargeTelegraph(Entity<ADTVetusComponent> ent, ref ADTMegafaunaChargeTelegraphEvent args)
    {
        var targetCoords = args.TargetEntity is { } target && !TerminatingOrDeleted(target)
            ? _transform.GetMapCoordinates(target)
            : args.Target;

        if (ent.Comp.Mode == ADTVetusMode.Bluespace)
            Teleport(ent, targetCoords);

        DestroySurroundings(ent);

        if (!_map.TryFindGridAt(targetCoords, out var gridUid, out var grid))
            return;

        var ownCoords = _transform.GetMapCoordinates(ent);
        var chargeTile = _map.TileIndicesFor(gridUid, grid, targetCoords);
        var ownTile = _map.TileIndicesFor(gridUid, grid, ownCoords);
        var dir = new Vector2i(Math.Sign(chargeTile.X - ownTile.X), Math.Sign(chargeTile.Y - ownTile.Y));
        var destination = chargeTile + dir * ent.Comp.ChargePast;

        args.Target = _transform.ToMapCoordinates(_map.GridTileToLocal(gridUid, grid, destination));
        args.Handled = true;
    }

    private void OnChargeEnded(Entity<ADTVetusComponent> ent, ref ADTMegafaunaChargeEndedEvent args)
    {
        if (TryComp<ADTMegafaunaPendingChargeComponent>(ent, out var pending) && pending.Queue.Count > 0)
            return;

        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRecoveryTime((ent.Owner, ai), ent.Comp.ChargeRecovery);
    }

    private void OnChargeTrampled(Entity<ADTVetusComponent> ent, ref ADTMegafaunaChargeTrampledEvent args)
    {
        var victim = args.Target;

        _audio.PlayPvs(ent.Comp.ChargeImpactSound, victim);
        _popup.PopupEntity(Loc.GetString("adt-vetus-charge-hit", ("vetus", ent.Owner)), victim, victim, PopupType.LargeCaution);

        if (ent.Comp.Mode != ADTVetusMode.Grav)
            return;

        var away = _transform.GetMapCoordinates(victim).Position - _transform.GetMapCoordinates(ent).Position;
        away = away.LengthSquared() < 0.01f ? args.Direction : Vector2.Normalize(away);

        _throwing.TryThrow(victim, away * ent.Comp.GravThrowDistance, ent.Comp.GravThrowSpeed, ent, recoil: false);
    }

    private void OnVetusMove(Entity<ADTVetusComponent> ent, ref MoveEvent args)
    {
        var xform = args.Component;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        if (ent.Comp.LastSmashTile != tile)
        {
            ent.Comp.LastSmashTile = tile;

            if (_megafauna.TryGetTarget(ent, out _))
                DestroySurroundings(ent);
        }

        if (!HasComp<ADTMegafaunaActiveChargeComponent>(ent) || ent.Comp.LastTrailTile == tile)
            return;

        ent.Comp.LastTrailTile = tile;

        switch (ent.Comp.Mode)
        {
            case ADTVetusMode.Pyro:
                PyroTrail(ent, gridUid, grid, tile);
                break;
            case ADTVetusMode.Vortex:
                var coords = _transform.ToMapCoordinates(_map.GridTileToLocal(gridUid, grid, tile));
                _explosion.QueueExplosion(
                    coords,
                    "Default",
                    ent.Comp.VortexTrailIntensity,
                    1f,
                    ent.Comp.VortexTrailIntensity,
                    ent,
                    tileBreakScale: 0f,
                    canCreateVacuum: false,
                    addLog: false);
                break;
        }
    }

    private void PyroTrail(Entity<ADTVetusComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        _drakeEffects.SpawnLavaWarning(ent.Comp.LavaWarning, gridUid, grid, tile, ent.Comp.LavaResetTime);

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var around = tile + new Vector2i(x, y);

                if (!_map.TryGetTileRef(gridUid, grid, around, out var tileRef) || tileRef.Tile.IsEmpty)
                    continue;

                Spawn(ent.Comp.PyroTrailFire, _map.GridTileToLocal(gridUid, grid, around));
                _atmos.HotspotExpose(gridUid, around, ent.Comp.PyroTrailExposeTemperature, ent.Comp.PyroTrailExposeVolume, ent);
            }
        }
    }

    private void Teleport(Entity<ADTVetusComponent> ent, MapCoordinates center)
    {
        var spots = GetFreeTilesAround(center, ent.Comp.TeleportRange, -1, false);

        if (spots.Count == 0)
            return;

        var destination = _random.Pick(spots);

        Spawn(ent.Comp.TeleportEffect, _transform.GetMapCoordinates(ent));
        _popup.PopupEntity(Loc.GetString("adt-vetus-teleport", ("vetus", ent.Owner)), ent, PopupType.MediumCaution);

        _transform.SetMapCoordinates(ent.Owner, destination);

        Spawn(ent.Comp.TeleportEffect, destination);
        _audio.PlayPvs(ent.Comp.TeleportSound, ent);
    }
}
