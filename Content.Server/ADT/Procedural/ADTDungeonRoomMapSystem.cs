using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Procedural;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Server.ADT.Procedural;

public sealed class ADTDungeonRoomMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _conf = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    private const int ChunkSize = 16;

    private const int TileStructSize = 7;

    private const int GridUid = 1;

    public bool TryBuild(ADTDungeonRoomPrototype room, out MappingDataNode data, out string error)
    {
        data = new MappingDataNode();
        error = string.Empty;

        if (room.Size.X <= 0 || room.Size.Y <= 0)
        {
            error = Loc.GetString("cmd-adt-dungeonroomtomap-bad-size", ("room", room.ID));
            return false;
        }

        var tileIds = new Dictionary<string, int> { ["Space"] = 0 };
        var chunks = new Dictionary<Vector2i, byte[]>();

        if (!TryFillChunks(room, tileIds, chunks, out error))
            return false;

        var entities = new SequenceDataNode();
        var uid = GridUid + 1;

        entities.Add(WriteGrid(room, chunks));

        foreach (var group in room.Entities)
        {
            var node = new MappingDataNode
            {
                ["proto"] = new ValueDataNode(group.Proto.Id),
                ["entities"] = WriteGroup(group, ref uid),
            };

            entities.Add(node);
        }

        data = new MappingDataNode
        {
            ["meta"] = WriteMeta(room, uid - 1),
            ["maps"] = OneId(GridUid),
            ["grids"] = OneId(GridUid),
            ["orphans"] = new SequenceDataNode(),
            ["nullspace"] = new SequenceDataNode(),
            ["tilemap"] = WriteTileMap(tileIds),
            ["entities"] = entities,
        };

        return true;
    }

    private bool TryFillChunks(
        ADTDungeonRoomPrototype room,
        Dictionary<string, int> tileIds,
        Dictionary<Vector2i, byte[]> chunks,
        out string error)
    {
        error = string.Empty;

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

                if (!_tileDefManager.TryGetDefinition(legendTile.Tile.Id, out _))
                {
                    error = Loc.GetString("cmd-adt-dungeonroomtomap-bad-tile",
                        ("room", room.ID),
                        ("tile", legendTile.Tile.Id));
                    return false;
                }

                if (!tileIds.TryGetValue(legendTile.Tile.Id, out var tileId))
                {
                    tileId = tileIds.Count;
                    tileIds[legendTile.Tile.Id] = tileId;
                }

                var chunkIndex = new Vector2i(x / ChunkSize, y / ChunkSize);

                if (!chunks.TryGetValue(chunkIndex, out var bytes))
                {
                    bytes = new byte[ChunkSize * ChunkSize * TileStructSize];
                    chunks[chunkIndex] = bytes;
                }

                var local = (y % ChunkSize) * ChunkSize + (x % ChunkSize);
                var offset = local * TileStructSize;

                BitConverter.TryWriteBytes(bytes.AsSpan(offset, sizeof(int)), tileId);
                bytes[offset + 4] = 0;
                bytes[offset + 5] = legendTile.Variant;
                bytes[offset + 6] = legendTile.Rotation;
            }
        }

        if (chunks.Count != 0)
            return true;

        error = Loc.GetString("cmd-adt-dungeonroomtomap-empty", ("room", room.ID));
        return false;
    }

    private MappingDataNode WriteGrid(ADTDungeonRoomPrototype room, Dictionary<Vector2i, byte[]> chunks)
    {
        var components = new SequenceDataNode();

        components.Add(new MappingDataNode
        {
            ["type"] = new ValueDataNode("MetaData"),
            ["name"] = new ValueDataNode(room.ID),
        });

        components.Add(Component("Transform"));
        components.Add(new MappingDataNode
        {
            ["type"] = new ValueDataNode("Map"),
            ["mapPaused"] = new ValueDataNode("True"),
        });

        components.Add(Component("GridTree"));
        components.Add(Component("Broadphase"));
        components.Add(Component("OccluderTree"));

        var chunkNode = new MappingDataNode();

        foreach (var (index, bytes) in chunks.OrderBy(pair => pair.Key.Y).ThenBy(pair => pair.Key.X))
        {
            var key = $"{index.X},{index.Y}";

            chunkNode[key] = new MappingDataNode
            {
                ["ind"] = new ValueDataNode(key),
                ["tiles"] = new ValueDataNode(Convert.ToBase64String(bytes)),
                ["version"] = new ValueDataNode("7"),
            };
        }

        components.Add(new MappingDataNode
        {
            ["type"] = new ValueDataNode("MapGrid"),
            ["chunks"] = chunkNode,
        });

        components.Add(new MappingDataNode
        {
            ["type"] = new ValueDataNode("Physics"),
            ["bodyStatus"] = new ValueDataNode("InAir"),
            ["angularDamping"] = new ValueDataNode("0.05"),
            ["linearDamping"] = new ValueDataNode("0.05"),
            ["fixedRotation"] = new ValueDataNode("False"),
            ["canCollide"] = new ValueDataNode("False"),
            ["bodyType"] = new ValueDataNode("Dynamic"),
        });

        components.Add(new MappingDataNode
        {
            ["type"] = new ValueDataNode("Fixtures"),
            ["fixtures"] = new MappingDataNode(),
        });

        components.Add(Component("SpreaderGrid"));
        components.Add(Component("GridPathfinding"));
        components.Add(WriteDecals(room));
        components.Add(Component("GasTileOverlay"));
        components.Add(Component("RadiationGridResistance"));
        components.Add(Component("NavMap"));

        var grid = new MappingDataNode
        {
            ["uid"] = new ValueDataNode(GridUid.ToString(CultureInfo.InvariantCulture)),
            ["components"] = components,
        };

        return new MappingDataNode
        {
            ["proto"] = new ValueDataNode(string.Empty),
            ["entities"] = new SequenceDataNode { grid },
        };
    }

    private MappingDataNode WriteDecals(ADTDungeonRoomPrototype room)
    {
        var component = Component("DecalGrid");

        if (room.Decals.Count == 0)
            return component;

        var nodes = new SequenceDataNode();
        var decalUid = 0;

        foreach (var group in room.Decals)
        {
            var settings = new MappingDataNode();

            if (group.Angle != Angle.Zero)
                settings["angle"] = new ValueDataNode($"{Number(group.Angle.Theta)} rad");

            if (group.ZIndex != 0)
                settings["zIndex"] = new ValueDataNode(group.ZIndex.ToString(CultureInfo.InvariantCulture));

            if (group.Cleanable)
                settings["cleanable"] = new ValueDataNode("True");

            settings["color"] = new ValueDataNode($"#{group.Color.ToHex().TrimStart('#')}");
            settings["id"] = new ValueDataNode(group.Id.Id);

            var decals = new MappingDataNode();

            foreach (var position in group.Positions)
            {
                decals[decalUid.ToString(CultureInfo.InvariantCulture)] = new ValueDataNode(Vector(position));
                decalUid++;
            }

            nodes.Add(new MappingDataNode
            {
                ["node"] = settings,
                ["decals"] = decals,
            });
        }

        component["chunkCollection"] = new MappingDataNode
        {
            ["version"] = new ValueDataNode("2"),
            ["nodes"] = nodes,
        };

        return component;
    }

    private static SequenceDataNode WriteGroup(ADTDungeonRoomEntities group, ref int uid)
    {
        var result = new SequenceDataNode();

        foreach (var position in group.Positions)
        {
            var transform = Component("Transform");
            transform["pos"] = new ValueDataNode(Vector(position));

            if (group.Rotation != Angle.Zero)
                transform["rot"] = new ValueDataNode($"{Number(group.Rotation.Theta)} rad");

            transform["parent"] = new ValueDataNode(GridUid.ToString(CultureInfo.InvariantCulture));

            if (group.Anchored is { } anchored)
                transform["anchored"] = new ValueDataNode(anchored ? "True" : "False");

            var components = new SequenceDataNode { transform };

            if (group.Components is { Count: > 0 } overrides)
            {
                foreach (var node in overrides.Node)
                {
                    components.Add(node.Copy());
                }
            }

            var entity = new MappingDataNode
            {
                ["uid"] = new ValueDataNode(uid.ToString(CultureInfo.InvariantCulture)),
                ["components"] = components,
            };

            if (group.MissingComponents.Count > 0)
            {
                var missing = new SequenceDataNode();

                foreach (var name in group.MissingComponents)
                {
                    missing.Add(new ValueDataNode(name));
                }

                entity["missingComponents"] = missing;
            }

            result.Add(entity);

            uid++;
        }

        return result;
    }

    private MappingDataNode WriteMeta(ADTDungeonRoomPrototype room, int entityCount)
    {
        return new MappingDataNode
        {
            ["format"] = new ValueDataNode(EntitySerializer.MapFormatVersion.ToString(CultureInfo.InvariantCulture)),
            ["category"] = new ValueDataNode(FileCategory.Map.ToString()),
            ["engineVersion"] = new ValueDataNode(_conf.GetCVar(CVars.BuildEngineVersion)),
            ["forkId"] = new ValueDataNode(_conf.GetCVar(CVars.BuildForkId)),
            ["forkVersion"] = new ValueDataNode(_conf.GetCVar(CVars.BuildVersion)),
            ["time"] = new ValueDataNode(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            ["entityCount"] = new ValueDataNode(entityCount.ToString(CultureInfo.InvariantCulture)),
        };
    }

    private static MappingDataNode WriteTileMap(Dictionary<string, int> tileIds)
    {
        var result = new MappingDataNode();

        foreach (var (name, id) in tileIds.OrderBy(pair => pair.Value))
        {
            result[id.ToString(CultureInfo.InvariantCulture)] = new ValueDataNode(name);
        }

        return result;
    }

    private static SequenceDataNode OneId(int id)
    {
        return new SequenceDataNode { new ValueDataNode(id.ToString(CultureInfo.InvariantCulture)) };
    }

    private static MappingDataNode Component(string name)
    {
        return new MappingDataNode { ["type"] = new ValueDataNode(name) };
    }

    private static string Number(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string Vector(Vector2 value)
    {
        return $"{Number(value.X)},{Number(value.Y)}";
    }
}
