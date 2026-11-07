using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Heretic.Prototypes;

public sealed class LenientRitualBehaviorListSerializer : ITypeReader<List<RitualCustomBehavior>, SequenceDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<RitualCustomBehavior>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<RitualCustomBehavior> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<RitualCustomBehavior>>? instanceProvider = null)
    {
        var list = instanceProvider != null ? instanceProvider() : new List<RitualCustomBehavior>();

        foreach (var dataNode in node.Sequence)
        {
            try
            {
                list.Add(serializationManager.Read<RitualCustomBehavior>(dataNode, hookCtx, context, notNullableOverride: true));
            }
            catch
            {
                list.Add(new UnknownRitualBehavior());
            }
        }

        return list;
    }
}
