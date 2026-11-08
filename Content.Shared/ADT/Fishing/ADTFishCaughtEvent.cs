using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Fishing;

[Serializable, NetSerializable]
public sealed class ADTFishCaughtEvent : EntityEventArgs
{
    public readonly NetEntity User;
    public readonly NetCoordinates From;
    public readonly EntProtoId Fish;

    public ADTFishCaughtEvent(NetEntity user, NetCoordinates from, EntProtoId fish)
    {
        User = user;
        From = from;
        Fish = fish;
    }
}
