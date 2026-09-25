using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland;

[Serializable, NetSerializable]
public sealed partial class ADTGraceCutDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ADTBaitDigDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetCoordinates Location;

    private ADTBaitDigDoAfterEvent()
    {
    }

    public ADTBaitDigDoAfterEvent(NetCoordinates location)
    {
        Location = location;
    }

    public override DoAfterEvent Clone() => this;
}
