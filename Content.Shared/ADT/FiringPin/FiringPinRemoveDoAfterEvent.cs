using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.FiringPin;

[Serializable, NetSerializable]
public sealed partial class FiringPinRemoveDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Pin;
}