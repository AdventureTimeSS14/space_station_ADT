using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Decals;

/// <summary>
///     Sent by clients to request that a single specific decal is removed on the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestSingleDecalRemovalEvent : EntityEventArgs
{
    public NetCoordinates Coordinates;
    public string DecalId;

    public RequestSingleDecalRemovalEvent(NetCoordinates coordinates, string decalId)
    {
        Coordinates = coordinates;
        DecalId = decalId;
    }
}
