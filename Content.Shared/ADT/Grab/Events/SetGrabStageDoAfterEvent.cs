using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Grab;

[Serializable, NetSerializable]
public sealed partial class SetGrabStageDoAfterEvent : SimpleDoAfterEvent
{
    public int Direction;

    public SetGrabStageDoAfterEvent(int direction)
    {
        Direction = direction;
    }
}
