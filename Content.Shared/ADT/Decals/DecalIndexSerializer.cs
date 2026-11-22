using System.Globalization;
using Content.Shared.Decals;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ADT.Decals;

/// <summary>
/// Записывает <see cref="DecalIndex"/> как "x,y:id", для сохранения в карту. Так как без него планеты с декалями не сохраняются.
/// </summary>
[TypeSerializer]
public sealed class DecalIndexSerializer : ITypeSerializer<DecalIndex, ValueDataNode>, ITypeCopyCreator<DecalIndex>
{
    public DecalIndex Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DecalIndex>? instanceProvider = null)
    {
        if (!TryParse(node.Value, out var index))
            throw new InvalidMappingException($"[DecalIndexSerializer] Could not parse {nameof(DecalIndex)}: '{node.Value}'");

        return index;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return TryParse(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, $"[DecalIndexSerializer] Failed parsing {nameof(DecalIndex)}.");
    }

    public DataNode Write(ISerializationManager serializationManager, DecalIndex value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(
            $"{value.Chunk.X.ToString(CultureInfo.InvariantCulture)}," +
            $"{value.Chunk.Y.ToString(CultureInfo.InvariantCulture)}:" +
            $"{value.Id.ToString(CultureInfo.InvariantCulture)}");
    }

    public DecalIndex CreateCopy(ISerializationManager serializationManager, DecalIndex source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return source;
    }

    private static bool TryParse(string value, out DecalIndex index)
    {
        index = default;

        var colon = value.IndexOf(':');
        var comma = value.IndexOf(',');
        if (comma <= 0 || colon <= comma)
            return false;

        if (!int.TryParse(value.AsSpan(0, comma), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(value.AsSpan(comma + 1, colon - comma - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
            !ushort.TryParse(value.AsSpan(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return false;
        }

        index = new DecalIndex(new Vector2i(x, y), id);
        return true;
    }
}
