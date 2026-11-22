// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Atmos;

[Serializable, NetSerializable]
public sealed class RMCStopDropRollVisualsNetworkEvent(NetEntity user, TimeSpan length) : EntityEventArgs
{
    public readonly NetEntity User = user;
    public readonly TimeSpan Length = length;
}
