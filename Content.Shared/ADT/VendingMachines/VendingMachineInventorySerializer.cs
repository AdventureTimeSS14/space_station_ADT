using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ADT.VendingMachines;
public sealed class VendingMachineInventorySerializer :
    ITypeSerializer<Dictionary<string, VendingMachineInventoryData>, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var protoMan = dependencies.Resolve<IPrototypeManager>();
        var mapping = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (key, valNode) in node.Children)
        {
            var keyNode = new ValueDataNode(key);
            ValidationNode keyValidation;
            ValidationNode valValidation;

            if (valNode is MappingDataNode)
            {
                keyValidation = protoMan.HasIndex<VendingMachineCategoryPrototype>(key)
                    ? new ValidatedValueNode(keyNode)
                    : new ErrorNode(keyNode, $"Vending category {key} was not found!");

                valValidation = serializationManager.ValidateNode<Dictionary<string, uint>, MappingDataNode,
                    PrototypeIdDictionarySerializer<uint, EntityPrototype>>((MappingDataNode)valNode, context);
            }
            else
            {
                keyValidation = protoMan.HasIndex<EntityPrototype>(key)
                    ? new ValidatedValueNode(keyNode)
                    : new ErrorNode(keyNode, $"Entity prototype {key} was not found!");

                valValidation = serializationManager.ValidateNode<uint>(valNode, context);
            }

            mapping.Add(keyValidation, valValidation);
        }

        return new ValidatedMappingNode(mapping);
    }

    public Dictionary<string, VendingMachineInventoryData> Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<string, VendingMachineInventoryData>>? instanceProvider = null)
    {
        var result = new Dictionary<string, VendingMachineInventoryData>();

        foreach (var (key, valNode) in node.Children)
        {
            if (valNode is MappingDataNode mapping)
            {
                result[key] = new VendingMachineInventoryData
                {
                    Items = serializationManager.Read<Dictionary<string, uint>>(mapping, hookCtx, context, notNullableOverride: true)
                };
            }
            else
            {
                result[key] = new VendingMachineInventoryData
                {
                    Amount = serializationManager.Read<uint>(valNode, hookCtx, context)
                };
            }
        }

        return result;
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<string, VendingMachineInventoryData> value,
        IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var node = new MappingDataNode();

        foreach (var (key, data) in value)
        {
            if (data.Items != null)
                node.Add(key, serializationManager.WriteValue(data.Items, alwaysWrite, context, false));
            else
                node.Add(key, new ValueDataNode(data.Amount.ToString()));
        }

        return node;
    }
}
