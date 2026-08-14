using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.AshWalker;

[Serializable, NetSerializable]
public enum ADTNecropolisCompassUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public struct ADTPointOfInterestEntry
{
    public NetEntity Entity;
    public string Name;
    public EntProtoId? Proto;

    public ADTPointOfInterestEntry(NetEntity entity, string name, EntProtoId? proto)
    {
        Entity = entity;
        Name = name;
        Proto = proto;
    }
}

[Serializable, NetSerializable]
public sealed class ADTNecropolisCompassBuiState : BoundUserInterfaceState
{
    public List<ADTPointOfInterestEntry> Points;

    public ADTNecropolisCompassBuiState(List<ADTPointOfInterestEntry> points)
    {
        Points = points;
    }
}

[Serializable, NetSerializable]
public sealed class ADTNecropolisCompassSelectMessage : BoundUserInterfaceMessage
{
    public NetEntity Point;

    public ADTNecropolisCompassSelectMessage(NetEntity point)
    {
        Point = point;
    }
}
