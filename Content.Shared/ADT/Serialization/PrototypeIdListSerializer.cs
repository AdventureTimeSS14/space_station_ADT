using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

public sealed class PrototypeIdListSerializer<T> : ITypeSerializer<List<string>, SequenceDataNode>, ITypeCopyCreator<List<string>>
    where T : class, IPrototype
{
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            if (elem is not ValueDataNode value)
            {
                list.Add(new ErrorNode(elem, "Invalid prototype id."));
                continue;
            }

            list.Add(ProtoIdSerializer<T>.Validate(dependencies, value));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<string> Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<string>>? instanceProvider = null)
    {
        var list = instanceProvider != null ? instanceProvider() : new List<string>();
        foreach (var elem in node.Sequence)
        {
            if (elem is ValueDataNode value)
                list.Add(value.Value);
        }

        return list;
    }

    public DataNode Write(ISerializationManager serializationManager, List<string> value,
        IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();
        foreach (var id in value)
            sequence.Add(new ValueDataNode(id));

        return sequence;
    }

    public List<string> CreateCopy(ISerializationManager serializationManager, List<string> source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new List<string>(source);
    }
}
