using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server.Decals;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Procedural;

public sealed class ADTDungeonRoomExportSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    private const string LegendChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+=/()<>;:~^";

    private const char SpaceChar = '.';

    private static readonly HashSet<string> SkippedComponents = new()
    {
        "Transform",
        "MetaData",
        "ContainerContainer",
        "Storage",
        "EntityStorage",
        "DeviceLinkSource",
        "DeviceLinkSink",
        "DeviceList",
        "Physics",
        "Fixtures",
    };

    public bool TrySerialize(
        Entity<MapGridComponent> grid,
        string roomId,
        IEnumerable<string> tags,
        out string yaml,
        out string error)
    {
        yaml = string.Empty;
        error = string.Empty;

        var tiles = _maps.GetAllTiles(grid.Owner, grid.Comp).ToList();

        if (tiles.Count == 0)
        {
            error = Loc.GetString("cmd-adt-converttodungeonroom-empty");
            return false;
        }

        var min = new Vector2i(int.MaxValue, int.MaxValue);
        var max = new Vector2i(int.MinValue, int.MinValue);

        foreach (var tile in tiles)
        {
            min = Vector2i.ComponentMin(min, tile.GridIndices);
            max = Vector2i.ComponentMax(max, tile.GridIndices);
        }

        var size = max - min + Vector2i.One;

        var legend = new Dictionary<(string Tile, byte Variant, byte Rotation), char>();
        var legendOrder = new List<(string Tile, byte Variant, byte Rotation)>();
        var map = new Dictionary<Vector2i, char>();

        foreach (var tile in tiles)
        {
            if (!_tileDefManager.TryGetDefinition(tile.Tile.TypeId, out var def))
                continue;

            var key = (def.ID, tile.Tile.Variant, tile.Tile.RotationMirroring);

            if (!legend.TryGetValue(key, out var symbol))
            {
                if (legendOrder.Count >= LegendChars.Length)
                {
                    error = Loc.GetString("cmd-adt-converttodungeonroom-too-many-tiles");
                    return false;
                }

                symbol = LegendChars[legendOrder.Count];
                legend[key] = symbol;
                legendOrder.Add(key);
            }

            map[tile.GridIndices - min] = symbol;
        }

        var builder = new StringBuilder();

        builder.Append("# Комната данжа ").Append(roomId).Append(", выгружена командой convertToDungeonRoom.\n");
        builder.Append("- type: adtDungeonRoom\n");
        builder.Append("  id: ").Append(roomId).Append('\n');
        builder.Append("  size: ").Append(size.X).Append(", ").Append(size.Y).Append('\n');

        var tagList = tags.ToList();

        if (tagList.Count > 0)
        {
            builder.Append("  tags:\n");
            foreach (var tag in tagList)
            {
                builder.Append("  - ").Append(tag).Append('\n');
            }
        }

        builder.Append("  legend:\n");
        foreach (var key in legendOrder)
        {
            builder.Append("    \"").Append(legend[key]).Append("\":\n");
            builder.Append("      tile: ").Append(key.Tile).Append('\n');

            if (key.Variant != 0)
                builder.Append("      variant: ").Append(key.Variant).Append('\n');

            if (key.Rotation != 0)
                builder.Append("      rotation: ").Append(key.Rotation).Append('\n');
        }

        builder.Append("  tiles:\n");
        for (var y = size.Y - 1; y >= 0; y--)
        {
            builder.Append("  - \"");
            for (var x = 0; x < size.X; x++)
            {
                builder.Append(map.TryGetValue(new Vector2i(x, y), out var symbol) ? symbol : SpaceChar);
            }
            builder.Append("\"\n");
        }

        AppendEntities(builder, grid, min);
        AppendDecals(builder, grid, min, size);

        yaml = builder.ToString();
        return true;
    }

    private void AppendEntities(StringBuilder builder, Entity<MapGridComponent> grid, Vector2i min)
    {
        var groups = new Dictionary<(string Proto, double Rotation, bool Anchored, string Components), List<Vector2>>();
        var children = Transform(grid.Owner).ChildEnumerator;

        while (children.MoveNext(out var child))
        {
            if (_container.IsEntityInContainer(child))
                continue;

            var meta = MetaData(child);

            if (meta.EntityPrototype is not { } proto)
                continue;

            var xform = Transform(child);
            var key = (proto.ID, xform.LocalRotation.Theta, xform.Anchored, SerializeOverrides(child, proto));

            groups.GetOrNew(key).Add(xform.LocalPosition - min);
        }

        if (groups.Count == 0)
            return;

        builder.Append("  entities:\n");

        foreach (var (key, positions) in groups.OrderBy(pair => pair.Key.Proto))
        {
            builder.Append("  - proto: ").Append(key.Proto).Append('\n');

            if (key.Rotation != 0)
                builder.Append("    rot: ").Append(Number(key.Rotation)).Append(" rad\n");

            if (key.Anchored)
                builder.Append("    anchored: true\n");

            if (key.Components.Length > 0)
                builder.Append("    components:\n").Append(key.Components);

            builder.Append("    positions:\n");
            foreach (var position in positions)
            {
                builder.Append("    - ").Append(Vector(position)).Append('\n');
            }
        }
    }

    private string SerializeOverrides(EntityUid uid, EntityPrototype proto)
    {
        var builder = new StringBuilder();

        foreach (var component in EntityManager.GetComponents(uid))
        {
            var type = component.GetType();
            var registration = _factory.GetRegistration(type);

            if (registration.Unsaved || SkippedComponents.Contains(registration.Name))
                continue;

            MappingDataNode? node;

            try
            {
                if (proto.Components.TryGetValue(registration.Name, out var protoEntry))
                {
                    var protoNode = _serialization.WriteValueAs<MappingDataNode>(type, protoEntry.Component, alwaysWrite: true);
                    node = _serialization.WriteValueAs<MappingDataNode>(type, component, alwaysWrite: true);
                    node = node.Except(protoNode) as MappingDataNode;
                }
                else
                {
                    node = _serialization.WriteValueAs<MappingDataNode>(type, component, alwaysWrite: false);
                }
            }
            catch (Exception exception)
            {
                Log.Warning($"Не смог выгрузить компонент {registration.Name} у {ToPrettyString(uid)}: {exception.Message}");
                continue;
            }

            if (node == null || node.Children.Count == 0)
                continue;

            if (!CanRead(registration, proto, node))
            {
                Log.Warning($"Пропущен компонент {registration.Name} у {ToPrettyString(uid)}.");
                continue;
            }

            node.InsertAt(0, "type", new ValueDataNode(registration.Name));

            var lines = node.ToString().TrimEnd('\n', '\r').Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                builder.Append(i == 0 ? "    - " : "      ").Append(line).Append('\n');
            }
        }

        return builder.ToString();
    }

    private bool CanRead(ComponentRegistration registration, EntityPrototype proto, MappingDataNode node)
    {
        var data = node;

        if (proto.Components.TryGetValue(registration.Name, out var protoEntry))
            data = _serialization.CombineMappings(data, protoEntry.Mapping);

        try
        {
            _serialization.Read(registration.Type, data);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void AppendDecals(StringBuilder builder, Entity<MapGridComponent> grid, Vector2i min, Vector2i size)
    {
        var bounds = new Box2(min, min + size);
        var decals = _decals.GetDecalsIntersecting(grid.Owner, bounds);

        if (decals.Count == 0)
            return;

        var groups = new Dictionary<(string Id, Color Color, double Angle, int ZIndex, bool Cleanable), List<Vector2>>();

        foreach (var (_, decal) in decals)
        {
            var key = (decal.Id, decal.Color ?? Color.White, decal.Angle.Theta, decal.ZIndex, decal.Cleanable);
            groups.GetOrNew(key).Add(decal.Coordinates - min);
        }

        builder.Append("  decals:\n");

        foreach (var (key, positions) in groups.OrderBy(pair => pair.Key.Id))
        {
            builder.Append("  - id: ").Append(key.Id).Append('\n');
            builder.Append("    color: '#").Append(key.Color.ToHex().TrimStart('#')).Append("'\n");

            if (key.Angle != 0)
                builder.Append("    angle: ").Append(Number(key.Angle)).Append(" rad\n");

            if (key.ZIndex != 0)
                builder.Append("    zIndex: ").Append(key.ZIndex).Append('\n');

            if (key.Cleanable)
                builder.Append("    cleanable: true\n");

            builder.Append("    positions:\n");
            foreach (var position in positions)
            {
                builder.Append("    - ").Append(Vector(position)).Append('\n');
            }
        }
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
