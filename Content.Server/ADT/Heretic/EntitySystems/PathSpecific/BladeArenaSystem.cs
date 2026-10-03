//

using Content.Server.Atmos.Components;
using Content.Server.Chat.Managers;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.Heretic.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Doors.Components;
using Content.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.ADT.Heretic.Systems.PathSpecific.Blade;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class BladeArenaSystem : SharedBladeArenaSystem
{
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    [Dependency] private readonly EntityQuery<AirlockComponent> _airlockQuery = default!;
    [Dependency] private readonly EntityQuery<BladeArenaDetachedComponent> _detachedQuery = default!;

    private readonly List<TileRef> _tilesToConvert = new();
    private readonly HashSet<Entity<AirtightComponent>> _intersecting = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BladeArenaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BladeArenaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BladeArenaComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<BladeArenaComponent, EndCollideEvent>(OnEndCollide);

        SubscribeLocalEvent<HereticArenaParticipantComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnEndCollide(Entity<BladeArenaComponent> ent, ref EndCollideEvent args)
    {
        RemComp<InsideArenaComponent>(args.OtherEntity);
    }

    private void OnMobStateChanged(Entity<HereticArenaParticipantComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive || args.NewMobState <= args.OldMobState ||
            args.Origin is not { } origin || origin == ent.Owner ||
            !ParticipantQuery.TryComp(origin, out var victor) ||
            !IsInsideArena(ent) || !IsInsideArena(origin))
            return;

        _heretic.TryGetHereticComponent(origin, out var heretic, out var mind);

        if (!victor.IsVictor && mind != default && TryComp(mind, out Content.Shared.Mind.MindComponent? mindComp) &&
            _player.TryGetSessionById(mindComp.UserId, out var session))
        {
            var msg = Loc.GetString(heretic == null ? "blade-arena-crit-message" : "blade-arena-crit-message-heretic");
            _chatMan.ChatMessageToOne(ChatChannel.Server,
                msg,
                msg,
                default,
                false,
                session.Channel,
                Color.Purple);
        }

        if (!victor.IsVictor)
        {
            victor.IsVictor = true;
            Dirty(origin, victor);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BladeArenaOuterWallComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.Anchored)
                continue;

            _transform.AttachToGridOrMap(uid, xform);
            _transform.AnchorEntity(uid, xform);
        }
    }

    private void OnStartCollide(Entity<BladeArenaComponent> ent, ref StartCollideEvent args)
    {
        var uid = args.OtherEntity;

        if (!_whitelist.CheckBoth(uid, ent.Comp.ParticipantBlacklist, ent.Comp.ParticipantWhitelist))
            return;

        if (EnsureComp<InsideArenaComponent>(uid, out _))
            return;

        ent.Comp.Participants.Add(uid);
        EntityManager.AddComponents(uid, ent.Comp.ComponentsToAdd, false);
    }

    private void OnShutdown(Entity<BladeArenaComponent> ent, ref ComponentShutdown args)
    {
        foreach (var participant in ent.Comp.Participants)
        {
            if (TerminatingOrDeleted(participant))
                continue;

            EntityManager.RemoveComponents(participant, ent.Comp.ComponentsToAdd);
            RemComp<InsideArenaComponent>(participant);
        }

        if (TerminatingOrDeleted(ent.Comp.Grid) || !TryComp(ent.Comp.Grid, out MapGridComponent? grid))
            return;

        foreach (var uid in ent.Comp.SpawnedEntities)
        {
            if (!TerminatingOrDeleted(uid))
                Del(uid);
        }

        foreach (var uid in ent.Comp.DetachedEntities)
        {
            if (!_detachedQuery.TryComp(uid, out var comp))
                continue;

            var xform = Transform(uid);
            var meta = MetaData(uid);
            _transform.SetCoordinates((uid, xform, meta), comp.OriginalCoords, rotation: comp.OriginalRotation);
            _transform.AnchorEntity(uid, xform);
        }

        var tileId = _protoMan.Index(ent.Comp.Tile).TileId;

        foreach (var indices in ent.Comp.TilesToRestore)
        {
            if (TerminatingOrDeleted(ent.Comp.Grid))
                continue;

            var restoreCoords = _map.GridTileToLocal(ent.Comp.Grid.Value, grid, indices);
            if (!_turf.TryGetTileRef(restoreCoords, out var tileRef) || tileRef.Value.Tile.TypeId != tileId)
                continue;

            _tile.DeconstructTile(tileRef.Value, false);
        }
    }

    private void OnMapInit(Entity<BladeArenaComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Radius < 1)
        {
            QueueDel(ent);
            return;
        }

        var coords = _transform.GetMapCoordinates(ent);
        if (!_mapMan.TryFindGridAt(coords, out var grid, out var gridComp))
        {
            QueueDel(ent);
            return;
        }

        if (!_turf.TryGetTileRef(Transform(ent).Coordinates, out var centerTile))
        {
            QueueDel(ent);
            return;
        }

        var originIndices = centerTile.Value.GridIndices;
        Entity<MapGridComponent> gridEnt = (grid, gridComp);
        var bounds = _lookup.GetWorldBounds(centerTile.Value);
        bounds.Box = bounds.Box.Enlarged(ent.Comp.Radius).Scale(1f - 0.2f / ent.Comp.Radius);
        _intersecting.Clear();
        _lookup.GetEntitiesIntersecting(coords.MapId, bounds, _intersecting, LookupFlags.Static);
        foreach (var uid in _intersecting)
        {
            if (_tag.HasTag(uid, "Wall"))
            {
                DetachEntity(uid, originIndices, gridEnt, ent.Comp, ent.Comp.WallReplacement);
                continue;
            }

            if (_tag.HasTag(uid, ent.Comp.WindowTag))
            {
                DetachEntity(uid, originIndices, gridEnt, ent.Comp, ent.Comp.WindowReplacement);
                continue;
            }

            if (!_airlockQuery.HasComp(uid))
                continue;

            DetachEntity(uid, originIndices, gridEnt, ent.Comp, null);
        }

        for (var i = -ent.Comp.Radius; i < ent.Comp.Radius; i++)
        {
            var a = originIndices + new Vector2i(i, ent.Comp.Radius);
            var c = originIndices + new Vector2i(ent.Comp.Radius, -i);
            var b = originIndices + new Vector2i(-i, -ent.Comp.Radius);
            var d = originIndices + new Vector2i(-ent.Comp.Radius, i);
            SpawnOuterWall(a, ent.Comp, gridEnt);
            SpawnOuterWall(b, ent.Comp, gridEnt);
            SpawnOuterWall(c, ent.Comp, gridEnt);
            SpawnOuterWall(d, ent.Comp, gridEnt);
        }

        var shape = new PolygonShape();
        shape.SetAsBox(Box2.CenteredAround(Vector2.Zero, bounds.Box.Size));
        _fixtures.TryCreateFixture(ent,
            shape,
            "fix1",
            collisionLayer: ent.Comp.Layer,
            collisionMask: ent.Comp.Layer,
            hard: false);
        _physics.SetCanCollide(ent, true, force: true);
    }

    private void DetachEntity(EntityUid uid,
        Vector2i originIndices,
        Entity<MapGridComponent> grid,
        BladeArenaComponent arena,
        EntProtoId? replaceWith)
    {
        var xform = Transform(uid);
        if (xform.ParentUid != grid.Owner)
            return;

        var coords = xform.Coordinates;
        if (!_turf.TryGetTileRef(coords, out var tileRef))
            return;

        var indices = tileRef.Value.GridIndices;
        var relative = indices - originIndices;
        var detached = EnsureComp<BladeArenaDetachedComponent>(uid);
        detached.OriginalCoords = coords;
        detached.OriginalRotation = xform.LocalRotation;
        arena.DetachedEntities.Add(uid);
        _transform.DetachEntity(uid, xform);

        if (replaceWith is not { } replacement || _tag.HasAnyTag(uid, arena.NoReplaceTags) ||
            Math.Abs(relative.X) >= arena.Radius || Math.Abs(relative.Y) >= arena.Radius)
            return;

        SpawnEntity(replacement, coords, arena, grid);
    }

    private void SpawnOuterWall(Vector2i indices,
        BladeArenaComponent arena,
        Entity<MapGridComponent> grid)
    {
        var wallCoords = _map.GridTileToLocal(grid, grid, indices);
        var spawned = Spawn(arena.OuterWall, wallCoords);
        arena.SpawnedEntities.Add(spawned);
        if (_turf.GetTileRef(wallCoords) != null)
            _transform.AnchorEntity((spawned, Transform(spawned)), grid);
    }

    private void SpawnEntity(EntProtoId proto,
        EntityCoordinates coords,
        BladeArenaComponent arena,
        Entity<MapGridComponent> grid)
    {
        var spawned = Spawn(proto, coords);
        var xform = Transform(spawned);
        if (!xform.Anchored)
            _transform.AnchorEntity((spawned, xform), grid);
        arena.SpawnedEntities.Add(spawned);
    }

    public EntityUid? TrySpawnArena(EntityCoordinates coords,
        EntProtoId<BladeArenaComponent> proto,
        ProtoId<ContentTileDefinition> tileReplacement,
        int minRadius,
        int tileRadius)
    {
        if (!_mapMan.TryFindGridAt(_transform.ToMapCoordinates(coords), out var grid, out var gridComp))
            return null;

        if (!_turf.TryGetTileRef(coords, out var centerTile))
            return null;

        var center = centerTile.Value.GridIndices;

        _tilesToConvert.Clear();
        _tilesToConvert.Add(centerTile.Value);

        var max = GetGreatestDistAndTiles();

        if (max < minRadius)
            return null;

        var replacement = _protoMan.Index(tileReplacement);

        var arena = EntityManager.CreateEntityUninitialized(proto, coords);
        var comp = EnsureComp<BladeArenaComponent>(arena);
        comp.Radius = max;
        comp.Grid = grid;
        EntityManager.InitializeAndStartEntity(arena);

        comp.TilesToRestore.Clear();
        foreach (var tile in _tilesToConvert)
        {
            comp.TilesToRestore.Add(tile.GridIndices);
            _tile.ReplaceTile(tile, replacement);
        }

        return arena;

        int GetGreatestDistAndTiles()
        {
            var greatestDist = 0;

            for (var i = 1; i <= tileRadius; i++)
            {
                for (var j = -i; j < i; j++)
                {
                    var coords1 = _map.GridTileToLocal(grid, gridComp, center + new Vector2i(j, i));
                    var coords2 = _map.GridTileToLocal(grid, gridComp, center + new Vector2i(i, -j));
                    var coords3 = _map.GridTileToLocal(grid, gridComp, center + new Vector2i(-j, -i));
                    var coords4 = _map.GridTileToLocal(grid, gridComp, center + new Vector2i(-i, j));
                    if (!_turf.TryGetTileRef(coords1, out var tile1) ||
                        !_turf.TryGetTileRef(coords2, out var tile2) ||
                        !_turf.TryGetTileRef(coords3, out var tile3) ||
                        !_turf.TryGetTileRef(coords4, out var tile4))
                        return greatestDist;

                    _tilesToConvert.Add(tile1.Value);
                    _tilesToConvert.Add(tile2.Value);
                    _tilesToConvert.Add(tile3.Value);
                    _tilesToConvert.Add(tile4.Value);
                }

                greatestDist++;
            }

            return greatestDist;
        }
    }
}
