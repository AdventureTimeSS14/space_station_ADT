using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
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

            if (valNode is MappingDataNode categoryNode)
            {
                keyValidation = protoMan.HasIndex<VendingMachineCategoryPrototype>(key)
                    ? new ValidatedValueNode(keyNode)
                    : new ErrorNode(keyNode, $"Vending category {key} was not found!");

                var items = new Dictionary<ValidationNode, ValidationNode>();
                foreach (var (itemKey, itemNode) in categoryNode.Children)
                {
                    var itemKeyNode = new ValueDataNode(itemKey);
                    var itemKeyValidation = protoMan.HasIndex<EntityPrototype>(itemKey)
                        ? (ValidationNode) new ValidatedValueNode(itemKeyNode)
                        : new ErrorNode(itemKeyNode, $"Entity prototype {itemKey} was not found!");

                    ValidationNode itemValidation;
                    if (itemNode is MappingDataNode itemMapping)
                        itemValidation = ValidateItemMapping(serializationManager, itemMapping, context);
                    else
                        itemValidation = serializationManager.ValidateNode<uint>(itemNode, context);

                    items.Add(itemKeyValidation, itemValidation);
                }

                valValidation = new ValidatedMappingNode(items);
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

    private static ValidationNode ValidateItemMapping(ISerializationManager serializationManager, MappingDataNode node,
        ISerializationContext? context)
    {
        var fields = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (fieldKey, fieldNode) in node.Children)
        {
            var keyNode = new ValueDataNode(fieldKey);
            ValidationNode keyValidation;
            ValidationNode valueValidation;

            switch (fieldKey)
            {
                case "amount":
                    keyValidation = new ValidatedValueNode(keyNode);
                    valueValidation = serializationManager.ValidateNode<uint>(fieldNode, context);
                    break;
                case "price":
                    keyValidation = new ValidatedValueNode(keyNode);
                    valueValidation = serializationManager.ValidateNode<int>(fieldNode, context);
                    break;
                default:
                    keyValidation = new ErrorNode(keyNode, $"Unknown vending inventory item field {fieldKey}, expected amount or price!");
                    valueValidation = new ValidatedValueNode(fieldNode is ValueDataNode value ? value : new ValueDataNode(string.Empty));
                    break;
            }

            fields.Add(keyValidation, valueValidation);
        }

        return new ValidatedMappingNode(fields);
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
                var items = new Dictionary<string, VendingMachineItemData>();
                foreach (var (itemKey, itemNode) in mapping.Children)
                {
                    if (itemNode is MappingDataNode itemMapping)
                    {
                        var itemData = new VendingMachineItemData();

                        if (itemMapping.TryGet<ValueDataNode>("amount", out var amountNode) && amountNode != null)
                            itemData.Amount = serializationManager.Read<uint>(amountNode, hookCtx, context);

                        if (itemMapping.TryGet<ValueDataNode>("price", out var priceNode) && priceNode != null)
                            itemData.Price = serializationManager.Read<int>(priceNode, hookCtx, context);

                        items[itemKey] = itemData;
                    }
                    else
                    {
                        items[itemKey] = new VendingMachineItemData
                        {
                            Amount = serializationManager.Read<uint>(itemNode, hookCtx, context)
                        };
                    }
                }

                result[key] = new VendingMachineInventoryData
                {
                    Items = items
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
            {
                var itemsNode = new MappingDataNode();
                foreach (var (itemKey, itemData) in data.Items)
                {
                    if (itemData.Price == 0)
                    {
                        itemsNode.Add(itemKey, new ValueDataNode(itemData.Amount.ToString()));
                    }
                    else
                    {
                        var itemNode = new MappingDataNode();
                        if (itemData.Amount != 1)
                            itemNode.Add("amount", new ValueDataNode(itemData.Amount.ToString()));
                        itemNode.Add("price", new ValueDataNode(itemData.Price.ToString()));
                        itemsNode.Add(itemKey, itemNode);
                    }
                }

                node.Add(key, itemsNode);
            }
            else
            {
                node.Add(key, new ValueDataNode(data.Amount.ToString()));
            }
        }

        return node;
    }
}
