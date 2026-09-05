using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.VendingMachines;
public sealed class ADTVendingReturnedEjectEvent : EntityEventArgs
{
    public readonly string ItemProtoId;
    public readonly int Count;
    public readonly EntityCoordinates Coordinates;
    public readonly bool ThrowItem;
    public readonly Color? PaintColor;

    public ADTVendingReturnedEjectEvent(string itemProtoId, int count, EntityCoordinates coordinates, bool throwItem, Color? paintColor = null)
    {
        ItemProtoId = itemProtoId;
        Count = count;
        Coordinates = coordinates;
        ThrowItem = throwItem;
        PaintColor = paintColor;
    }
}