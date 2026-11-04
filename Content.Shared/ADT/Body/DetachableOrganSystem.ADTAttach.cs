using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Body;

public sealed partial class DetachableOrganSystem
{
    [PublicAPI]
    public bool Attach(Entity<DetachableOrganComponent?> organ, EntityUid targetBody, EntityUid parentOrgan)
    {
        if (!_detachableOrgan.Resolve(organ, ref organ.Comp, false) || !_organ.TryComp(organ, out var organComp))
            return false;

        if (!_container.TryGetContainer(targetBody, BodyComponent.ContainerID, out var targetContainer))
            return false;

        var oldBody = organComp.Body;

        var children = new List<EntityUid>();
        foreach (var child in _organRelation.AllChildren(organ.Owner))
            children.Add(child.Owner);

        if (!_container.Insert(organ.Owner, targetContainer, force: true))
            return false;

        foreach (var child in children)
        {
            if (!_container.Insert(child, targetContainer, force: true))
                Log.Error($"{ToPrettyString(child)} could not be transferred to body {ToPrettyString(targetBody)}.");
        }

        _organRelation.Relate(parentOrgan, organ.Owner);

        if (oldBody is { } old && old != targetBody)
            QueueDel(old);

        return true;
    }
}