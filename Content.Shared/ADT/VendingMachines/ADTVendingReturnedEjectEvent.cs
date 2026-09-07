using Robust.Shared.Map;

namespace Content.Shared.ADT.VendingMachines;
public sealed class ADTVendingReturnedEjectEvent : EntityEventArgs
{
    public readonly string ItemProtoId;
    public readonly int Count;
    public readonly EntityCoordinates Coordinates;
    public readonly bool ThrowItem;

    public ADTVendingReturnedEjectEvent(string itemProtoId, int count, EntityCoordinates coordinates, bool throwItem)
    {
        ItemProtoId = itemProtoId;
        Count = count;
        Coordinates = coordinates;
        ThrowItem = throwItem;
    }
}