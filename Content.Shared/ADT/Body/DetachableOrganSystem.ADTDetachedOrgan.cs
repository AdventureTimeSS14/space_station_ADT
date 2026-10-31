using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Body;

public sealed partial class DetachableOrganSystem {
    [PublicAPI]
    public bool TryGetDetachedOrgan(EntityUid detachedBody, out EntityUid organ)
    {
        organ = default;

        if (!_container.TryGetContainer(detachedBody, BodyComponent.ContainerID, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!_detachableOrgan.HasComp(contained))
                continue;

            organ = contained;
            return true;
        }

        return false;
    }
}