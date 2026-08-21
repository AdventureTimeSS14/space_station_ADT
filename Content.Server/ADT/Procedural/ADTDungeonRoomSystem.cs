using System.Numerics;
using Content.Server.Decals;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.Procedural;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Procedural;

public sealed class ADTDungeonRoomSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(Vector2i, Tile)> _tiles = new();
    private readonly List<ADTDungeonRoomPrototype> _availableRooms = new();
    private readonly Dictionary<ADTComponentOverrides, ComponentRegistry> _registryCache = new();

    public ADTDungeonRoomPrototype? GetRoom(
        IRobustRandom random,
        EntityWhitelist? whitelist = null,
        Vector2i? minSize = null,
        Vector2i? maxSize = null)
    {
        if (whitelist is { Tags: null })
            return null;

        _availableRooms.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<ADTDungeonRoomPrototype>())
        {
            if (minSize is not null && (proto.Size.X < minSize.Value.X || proto.Size.Y < minSize.Value.Y))
                continue;

            if (maxSize is not null && (proto.Size.X > maxSize.Value.X || proto.Size.Y > maxSize.Value.Y))
                continue;

            if (whitelist == null)
            {
                _availableRooms.Add(proto);
                continue;
            }

            foreach (var tag in whitelist.Tags)
            {
                if (!proto.Tags.Contains(tag))
                    continue;

                _availableRooms.Add(proto);
                break;
            }
        }

        if (_availableRooms.Count == 0)
            return null;

        return _availableRooms[random.Next(_availableRooms.Count)];
    }

    public Angle GetRoomRotation(ADTDungeonRoomPrototype room, IRobustRandom random)
    {
        if (room.Size.X == room.Size.Y)
            return random.Next(4) * Math.PI / 2;

        if (random.Next(2) == 1)
            return Math.PI;

        return Angle.Zero;
    }

    public void SpawnRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i origin,
        ADTDungeonRoomPrototype room,
        IRobustRandom random,
        bool clearExisting = false,
        bool rotation = false)
    {
        var originTransform = Matrix3Helpers.CreateTranslation(origin.X, origin.Y);
        var roomRotation = rotation ? GetRoomRotation(room, random) : Angle.Zero;
        var roomTransform = Matrix3Helpers.CreateTransform((Vector2)room.Size / 2f, roomRotation);
        var finalTransform = Matrix3x2.Multiply(roomTransform, originTransform);

        SpawnRoom(gridUid, grid, finalTransform, room, clearExisting);
    }

    public void SpawnRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Matrix3x2 roomTransform,
        ADTDungeonRoomPrototype room,
        bool clearExisting = false)
    {
        var finalRoomRotation = roomTransform.Rotation();
        var roomCenter = (Vector2)room.Size / 2f * grid.TileSize;
        var tileOffset = -roomCenter + grid.TileSizeHalfVector;

        SpawnTiles(gridUid, grid, roomTransform, room, tileOffset, clearExisting);
        SpawnAreas(gridUid, roomTransform, room, tileOffset);
        SpawnEntities(gridUid, grid, room, roomTransform, roomCenter, finalRoomRotation);
        SpawnDecals(gridUid, grid, room, roomTransform, roomCenter, finalRoomRotation);
    }

    private void SpawnAreas(
        EntityUid gridUid,
        Matrix3x2 roomTransform,
        ADTDungeonRoomPrototype room,
        Vector2 tileOffset)
    {
        if (room.Areas.Count == 0)
            return;

        var areaGrid = EnsureComp<AreaGridComponent>(gridUid);
        var touched = new HashSet<EntProtoId<AreaComponent>>();

        for (var y = 0; y < room.Size.Y; y++)
        {
            var rowIndex = room.Size.Y - 1 - y;

            if (rowIndex >= room.Areas.Count)
                continue;

            var row = room.Areas[rowIndex];

            for (var x = 0; x < room.Size.X && x < row.Length; x++)
            {
                if (!room.AreaLegend.TryGetValue(row[x].ToString(), out var area))
                    continue;

                var tilePos = Vector2.Transform(new Vector2(x, y) + tileOffset, roomTransform);

                areaGrid.Areas[tilePos.Floored()] = area;
                touched.Add(area);
            }
        }

        if (touched.Count == 0)
            return;

        Dirty(gridUid, areaGrid);

        foreach (var area in touched)
        {
            RefreshArea(gridUid, area);
        }
    }

    private void RefreshArea(EntityUid gridUid, EntProtoId<AreaComponent> area)
    {
        if (!_areas.TryGetAreaCenter(area, gridUid, out var center))
            return;

        if (TryFindArea(gridUid, area, out var existing))
            Del(existing);

        Spawn(area, center);
    }

    private bool TryFindArea(EntityUid gridUid, EntProtoId<AreaComponent> area, out EntityUid found)
    {
        found = default;

        var query = AllEntityQuery<AreaComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var xform))
        {
            if (xform.GridUid != gridUid)
                continue;

            if (meta.EntityPrototype?.ID != area.Id)
                continue;

            found = uid;
            return true;
        }

        return false;
    }

    private void SpawnTiles(
        EntityUid gridUid,
        MapGridComponent grid,
        Matrix3x2 roomTransform,
        ADTDungeonRoomPrototype room,
        Vector2 tileOffset,
        bool clearExisting)
    {
        _tiles.Clear();

        var quarterTurns = GetQuarterTurns(roomTransform.Rotation());

        for (var y = 0; y < room.Size.Y; y++)
        {
            var rowIndex = room.Size.Y - 1 - y;

            if (rowIndex >= room.Tiles.Count)
                continue;

            var row = room.Tiles[rowIndex];

            for (var x = 0; x < room.Size.X && x < row.Length; x++)
            {
                if (!room.Legend.TryGetValue(row[x].ToString(), out var legendTile))
                    continue;

                if (!_tileDefManager.TryGetDefinition(legendTile.Tile.Id, out var tileDef))
                {
                    Log.Error($"Комната {room.ID} ссылается на неизвестный тайл {legendTile.Tile.Id}");
                    continue;
                }

                var tilePos = Vector2.Transform(new Vector2(x, y) + tileOffset, roomTransform);
                var rounded = tilePos.Floored();

                var rotation = RotateTile(legendTile.Rotation, quarterTurns);

                _tiles.Add((rounded, new Tile(tileDef.TileId, 0, legendTile.Variant, rotation)));

                if (!clearExisting)
                    continue;

                foreach (var ent in _maps.GetAnchoredEntities((gridUid, grid), rounded))
                {
                    QueueDel(ent);
                }
            }
        }

        _maps.SetTiles(gridUid, grid, _tiles);
    }

    private static int GetQuarterTurns(Angle rotation)
    {
        return (int) Math.Round(rotation.Theta / (Math.PI / 2)) & 3;
    }

    private static byte RotateTile(byte rotationMirroring, int quarterTurns)
    {
        var mirrored = rotationMirroring & 4;
        var rotation = (rotationMirroring + quarterTurns) & 3;

        return (byte) (rotation | mirrored);
    }

    private void SpawnEntities(
        EntityUid gridUid,
        MapGridComponent grid,
        ADTDungeonRoomPrototype room,
        Matrix3x2 roomTransform,
        Vector2 roomCenter,
        Angle finalRoomRotation)
    {
        foreach (var group in room.Entities)
        {
            var overrides = GetOverrides(group);

            foreach (var position in group.Positions)
            {
                var childPos = Vector2.Transform(position - roomCenter, roomTransform);

                var ent = EntityManager.SpawnAttachedTo(
                    group.Proto,
                    new EntityCoordinates(gridUid, childPos),
                    overrides,
                    group.Rotation + finalRoomRotation);

                _transform.AttachToGridOrMap(ent);

                if (group.Anchored == null)
                    continue;

                var xform = Transform(ent);

                if (group.Anchored.Value && !xform.Anchored)
                    _transform.AnchorEntity((ent, xform), (gridUid, grid));
                else if (!group.Anchored.Value && xform.Anchored)
                    _transform.Unanchor(ent, xform);
            }
        }
    }

    private ComponentRegistry? GetOverrides(ADTDungeonRoomEntities group)
    {
        if (group.Components is not { Count: > 0 } overrides)
            return null;

        if (_registryCache.TryGetValue(overrides, out var cached))
            return cached;

        _prototype.TryIndex(group.Proto, out var entityProto);

        var registry = new ComponentRegistry();

        foreach (var element in overrides.Node)
        {
            if (element is not MappingDataNode node)
            {
                Log.Error($"Правка компонента у {group.Proto} не мапинг.");
                continue;
            }

            if (node.Get("type") is not ValueDataNode nameNode)
                continue;

            var name = nameNode.Value;

            if (!_factory.TryGetRegistration(name, out var registration))
            {
                Log.Error($"Комната ссылается на неизвестный компонент {name}");
                continue;
            }

            var data = node.Copy();
            data.Remove("type");

            if (entityProto != null && entityProto.Components.TryGetValue(name, out var protoData))
                data = _serialization.CombineMappings(data, protoData.Mapping);

            try
            {
                var component = (IComponent) _serialization.Read(registration.Type, data)!;
                registry[name] = new EntityPrototype.ComponentRegistryEntry(component, data);
            }
            catch (Exception exception)
            {
                Log.Error($"Не смог прочитать правку компонента {name} у {group.Proto}: {exception.Message}");
            }
        }

        _registryCache[overrides] = registry;
        return registry;
    }

    private void SpawnDecals(
        EntityUid gridUid,
        MapGridComponent grid,
        ADTDungeonRoomPrototype room,
        Matrix3x2 roomTransform,
        Vector2 roomCenter,
        Angle finalRoomRotation)
    {
        if (room.Decals.Count == 0)
            return;

        EnsureComp<DecalGridComponent>(gridUid);

        foreach (var group in room.Decals)
        {
            var angle = (group.Angle + finalRoomRotation).Reduced();

            foreach (var decalPosition in group.Positions)
            {
                var position = Vector2.Transform(decalPosition + grid.TileSizeHalfVector - roomCenter, roomTransform);
                position -= grid.TileSizeHalfVector;

                position += GetDecalRotationOffset(angle, group.Id);

                _decals.TryAddDecal(
                    group.Id,
                    new EntityCoordinates(gridUid, position),
                    out _,
                    group.Color,
                    angle,
                    group.ZIndex,
                    group.Cleanable);
            }
        }
    }

    private static Vector2 GetDecalRotationOffset(Angle angle, string decalId)
    {
        if (angle.Equals(Math.PI))
            return new Vector2(-1f / 32f, 1f / 32f);

        if (angle.Equals(-Math.PI / 2f))
            return new Vector2(-1f / 32f, 0f);

        if (angle.Equals(Math.PI / 2f))
            return new Vector2(0f, 1f / 32f);

        if (angle.Equals(Math.PI * 1.5f)
            && decalId != "DiagonalCheckerAOverlay"
            && decalId != "DiagonalCheckerBOverlay")
        {
            return new Vector2(-1f / 32f, 0f);
        }

        return Vector2.Zero;
    }
}
