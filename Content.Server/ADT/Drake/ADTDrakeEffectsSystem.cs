using System.Linq;
using System.Numerics;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Drake;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GatherableSystem _gatherable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Direction[] Cardinals =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    private const int DenseLayer = (int) (CollisionGroup.Opaque | CollisionGroup.Impassable);

    private readonly List<EntityUid> _tileBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeMeteorTargetComponent, MapInitEvent>(OnMeteorMapInit);
        SubscribeLocalEvent<ADTDrakeLavaWarningComponent, MapInitEvent>(OnLavaWarningMapInit);
        SubscribeLocalEvent<ADTDrakeTempLavaComponent, TimedDespawnEvent>(OnTempLavaDespawn);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        UpdateFireLines(now);
        UpdateMeteors(now);
        UpdateLavaWarnings(now);
    }

    public void StartFireLine(EntityUid? source, EntityUid grid, List<Vector2i> tiles, ADTDrakeComponent drake)
    {
        if (tiles.Count == 0)
            return;

        var line = Spawn(drake.FireLineProto, MapCoordinates.Nullspace);
        var comp = EnsureComp<ADTDrakeFireLineComponent>(line);
        comp.Source = source;
        comp.Grid = grid;
        comp.Tiles = tiles;
        comp.Index = 0;
        comp.StepDelay = drake.FireLineStepDelay;
        comp.NextStepAt = _timing.CurTime;
        comp.Damage = drake.FireLineDamage;
        comp.MechDamage = drake.FireLineMechDamage;
        comp.FireProto = drake.FireProto;
    }

    private void UpdateFireLines(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTDrakeFireLineComponent>();

        while (query.MoveNext(out var uid, out var line))
        {
            if (now < line.NextStepAt)
                continue;

            if (!TryComp<MapGridComponent>(line.Grid, out var grid) || line.Index >= line.Tiles.Count)
            {
                QueueDel(uid);
                continue;
            }

            var tile = line.Tiles[line.Index];
            line.Index++;
            line.NextStepAt = now + line.StepDelay;

            if (IsTileDense(line.Grid, grid, tile))
            {
                QueueDel(uid);
                continue;
            }

            BurnTile((uid, line), grid, tile);
        }
    }

    private void BurnTile(Entity<ADTDrakeFireLineComponent> line, MapGridComponent grid, Vector2i tile)
    {
        SpawnFire(line.Comp.FireProto, _map.GridTileToLocal(line.Comp.Grid, grid, tile));

        foreach (var target in GetEntitiesOnTile(line.Comp.Grid, grid, tile))
        {
            if (target == line.Comp.Source)
                continue;

            if (HasComp<MobStateComponent>(target))
            {
                if (!line.Comp.HitList.Add(target))
                    continue;

                Damage(target, "Heat", line.Comp.Damage, line.Comp.Source);

                _popup.PopupEntity(Loc.GetString("adt-drake-fire-line-hit"), target, target, PopupType.LargeCaution);

                continue;
            }

            if (HasComp<MechComponent>(target) && line.Comp.HitList.Add(target))
                Damage(target, "Blunt", line.Comp.MechDamage, line.Comp.Source);
        }
    }

    private void OnMeteorMapInit(Entity<ADTDrakeMeteorTargetComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ImpactAt = _timing.CurTime + ent.Comp.Delay;

        _audio.PlayPvs(ent.Comp.SpawnSound, ent);
        SpawnAttachedTo(ent.Comp.FallingProto, new EntityCoordinates(ent, Vector2.Zero));
    }

    private void UpdateMeteors(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTDrakeMeteorTargetComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var meteor, out var xform))
        {
            if (now < meteor.ImpactAt)
                continue;

            MeteorImpact((uid, meteor), xform);
            QueueDel(uid);
        }
    }

    private void MeteorImpact(Entity<ADTDrakeMeteorTargetComponent> ent, TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (HasComp<GatherableComponent>(anchored))
            {
                _gatherable.Gather(anchored);
                continue;
            }

            if (HasComp<OreVeinComponent>(anchored))
                QueueDel(anchored);
        }

        _audio.PlayPvs(ent.Comp.ImpactSound, xform.Coordinates);
        SpawnFire(ent.Comp.FireProto, _map.GridTileToLocal(gridUid, grid, tile));

        foreach (var target in GetEntitiesOnTile(gridUid, grid, tile))
        {
            if (!HasComp<MobStateComponent>(target))
                continue;

            if (HasComp<ADTDrakeComponent>(target))
                continue;

            Damage(target, "Heat", ent.Comp.Damage, null);
        }
    }

    public void SpawnLavaWarning(EntProtoId proto, EntityUid gridUid, MapGridComponent grid, Vector2i tile, TimeSpan resetTime)
    {
        var warning = Spawn(proto, _map.GridTileToLocal(gridUid, grid, tile));
        EnsureComp<ADTDrakeLavaWarningComponent>(warning).ResetTime = resetTime;
    }

    private void OnLavaWarningMapInit(Entity<ADTDrakeLavaWarningComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ImpactAt = _timing.CurTime + ent.Comp.Delay;
        _audio.PlayPvs(ent.Comp.SpawnSound, ent);
    }

    private void UpdateLavaWarnings(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTDrakeLavaWarningComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var warning, out var xform))
        {
            if (now < warning.ImpactAt)
                continue;

            LavaImpact((uid, warning), xform);
            QueueDel(uid);
        }
    }

    private void LavaImpact(Entity<ADTDrakeLavaWarningComponent> ent, TransformComponent xform)
    {
        _audio.PlayPvs(ent.Comp.ImpactSound, xform.Coordinates);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        foreach (var target in GetEntitiesOnTile(gridUid, grid, tile))
        {
            if (HasComp<MobStateComponent>(target))
            {
                if (HasComp<ADTDrakeComponent>(target))
                    continue;

                Damage(target, "Heat", ent.Comp.Damage, null);
                _popup.PopupEntity(Loc.GetString("adt-drake-lava-hit"), target, target, PopupType.LargeCaution);
                continue;
            }

            if (HasComp<MechComponent>(target))
                Damage(target, "Blunt", ent.Comp.MechDamage, null);
        }

        if (IsTileDense(gridUid, grid, tile) || HasLava(gridUid, grid, tile, ent.Comp.LavaPrototypes))
            return;

        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        var lava = Spawn(ent.Comp.TempLavaProto, coords);
        EnsureComp<TimedDespawnComponent>(lava).Lifetime = (float) ent.Comp.ResetTime.TotalSeconds;

        var tempLava = EnsureComp<ADTDrakeTempLavaComponent>(lava);

        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
        {
            if (!HasComp<ChasmComponent>(anchored))
                continue;

            if (MetaData(anchored).EntityPrototype is not { } proto)
                continue;

            tempLava.Replaced.Add(new ADTDrakeReplacedEntity
            {
                Prototype = proto.ID,
                Rotation = Transform(anchored).LocalRotation,
            });

            QueueDel(anchored);
        }
    }

    private void OnTempLavaDespawn(Entity<ADTDrakeTempLavaComponent> ent, ref TimedDespawnEvent args)
    {
        if (ent.Comp.Replaced.Count == 0)
            return;

        var coords = Transform(ent).Coordinates;

        foreach (var replaced in ent.Comp.Replaced)
        {
            var restored = Spawn(replaced.Prototype, coords);
            _transform.SetLocalRotation(restored, replaced.Rotation);
        }
    }

    public bool HasLava(EntityUid gridUid, MapGridComponent grid, Vector2i tile, List<EntProtoId> lavaPrototypes)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (MetaData(anchored).EntityPrototype is not { } proto)
                continue;

            foreach (var lava in lavaPrototypes)
            {
                if (proto.ID == lava)
                    return true;
            }
        }

        return false;
    }

    private void SpawnFire(EntProtoId proto, EntityCoordinates coords)
    {
        var fire = Spawn(proto, coords);
        _transform.SetLocalRotation(fire, _random.Pick(Cardinals).ToAngle());
    }

    public bool IsTileDense(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (!TryComp<PhysicsComponent>(anchored, out var physics) || !physics.CanCollide || !physics.Hard)
                continue;

            if (!TryComp<FixturesComponent>(anchored, out var fixtures))
                continue;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (fixture.Hard && (fixture.CollisionLayer & DenseLayer) == DenseLayer)
                    return true;
            }
        }

        return false;
    }

    private List<EntityUid> GetEntitiesOnTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        _tileBuffer.Clear();
        _tileBuffer.AddRange(_lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid));
        return _tileBuffer;
    }

    private void Damage(EntityUid target, string type, float amount, EntityUid? origin)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add(type, amount);
        _damageable.TryChangeDamage(target, damage, origin: origin);
    }
}
