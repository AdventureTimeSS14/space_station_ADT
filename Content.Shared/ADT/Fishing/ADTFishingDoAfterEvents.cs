using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Fishing;

[Serializable, NetSerializable]
public sealed partial class ADTFishingDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ADTCharredKrillDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ADTFishingCastActionEvent : WorldTargetActionEvent
{
}
