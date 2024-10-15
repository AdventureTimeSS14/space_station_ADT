using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ReliveResuscitation;

[Serializable, NetSerializable]
public sealed partial class ReliveDoAfterEvent : SimpleDoAfterEvent
{
}
