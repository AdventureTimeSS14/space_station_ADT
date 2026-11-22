using Content.Shared.ADT.UI;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.AshWalker;

[Serializable, NetSerializable]
public enum ADTNecropolisCompassUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTNecropolisCompassBuiState : BoundUserInterfaceState
{
    public List<ADTEntityPickerEntry> Points;

    public ADTNecropolisCompassBuiState(List<ADTEntityPickerEntry> points)
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
