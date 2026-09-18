using Robust.Shared.Containers;

namespace Content.Shared.ADT.VendingMachines;

public static class VendingMachineHelpers
{
    public static Dictionary<string, NetEntity> GetReturnedItemEntities(IEntityManager entMan, EntityUid uid)
    {
        var result = new Dictionary<string, NetEntity>();

        if (!entMan.TryGetComponent(uid, out ContainerManagerComponent? containers)
            || !containers.Containers.TryGetValue(VendingMachineComponent.ReturnedItemsContainerId, out var container))
        {
            return result;
        }

        foreach (var ent in container.ContainedEntities)
        {
            if (!entMan.TryGetComponent(ent, out MetaDataComponent? meta)
                || meta.EntityPrototype?.ID is not { } protoId)
            {
                continue;
            }

            result.TryAdd(protoId, entMan.GetNetEntity(ent));
        }

        return result;
    }
}