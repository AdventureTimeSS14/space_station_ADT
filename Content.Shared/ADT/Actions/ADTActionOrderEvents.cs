using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Actions;

[Serializable, NetSerializable]
public sealed class ADTActionOrderChangeEvent : EntityEventArgs
{
    public readonly List<EntProtoId> Order;
    public readonly List<EntProtoId> Removed;

    public ADTActionOrderChangeEvent(List<EntProtoId> order, List<EntProtoId> removed)
    {
        Order = order;
        Removed = removed;
    }
}
