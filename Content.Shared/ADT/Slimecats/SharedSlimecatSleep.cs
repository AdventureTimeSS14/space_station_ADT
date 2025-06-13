using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Slimecats;

public sealed partial class SharedSlimeCatSleepActionEvent : InstantActionEvent
{
}


[Serializable, NetSerializable]
public enum StateSlimcatVisual : byte
{
    Sleep
}