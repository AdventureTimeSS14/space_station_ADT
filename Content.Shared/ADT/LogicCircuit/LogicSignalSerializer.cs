using Content.Shared.ADT.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ADT.LogicCircuit;

[TypeSerializer]
public sealed class LogicSignalSerializer : ITypeSerializer<LogicSignal, ValueDataNode>, ITypeCopyCreator<LogicSignal>
{
    public LogicSignal Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<LogicSignal>? instanceProvider = null)
    {
        var maxLength = dependencies.Resolve<IConfigurationManager>().GetCVar(ADTCCVars.LogicMaxConfigLength);
        return LogicSignal.FromText(node.Value, maxLength);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        LogicSignal value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.AsText());
    }

    [MustUseReturnValue]
    public LogicSignal CreateCopy(
        ISerializationManager serializationManager,
        LogicSignal source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }
}
