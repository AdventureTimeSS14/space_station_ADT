using System.Numerics;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining;

[ByRefEvent]
public record struct ADTGatherAttemptEvent(EntityUid? Gatherer, bool Cancelled = false);

[ByRefEvent]
public record struct ADTMineralDrillEvent(int Power, EntityUid? User, bool ByExplosion, bool Handled = false);

[Serializable, NetSerializable]
public sealed partial class ADTMineralDrillDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class ADTMineralHitEvent : EntityEventArgs
{
    public readonly NetEntity Mineral;
    public readonly Vector2 Direction;

    public ADTMineralHitEvent(NetEntity mineral, Vector2 direction)
    {
        Mineral = mineral;
        Direction = direction;
    }
}
