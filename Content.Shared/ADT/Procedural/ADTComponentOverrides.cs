using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ADT.Procedural;

public sealed class ADTComponentOverrides
{
    public SequenceDataNode Node = new();

    public int Count => Node.Count;
}

[TypeSerializer]
public sealed class ADTComponentOverridesSerializer :
    ITypeSerializer<ADTComponentOverrides, SequenceDataNode>,
    ITypeCopyCreator<ADTComponentOverrides>
{
    public ADTComponentOverrides Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ADTComponentOverrides>? instanceProvider = null)
    {
        var value = instanceProvider != null ? instanceProvider() : new ADTComponentOverrides();
        value.Node = node.Copy();
        return value;
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        ADTComponentOverrides value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return value.Node.Copy();
    }

    public ADTComponentOverrides CreateCopy(
        ISerializationManager serializationManager,
        ADTComponentOverrides source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return new ADTComponentOverrides { Node = source.Node.Copy() };
    }
}
