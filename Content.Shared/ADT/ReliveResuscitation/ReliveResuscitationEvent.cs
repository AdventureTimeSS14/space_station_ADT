using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ReliveResuscitation;

/// <summary>
/// This event is triggered after the ReliveResuscitationComponent has been applied.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ReliveDoAfterEvent : SimpleDoAfterEvent
{
}
