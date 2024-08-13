using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Bible.Components;

public sealed partial class ChaplainMakeBelieverActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChaplainMakeBelieverDoAfter : SimpleDoAfterEvent
{
}

public sealed partial class ChaplainPrayersHandActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChaplainPrayersHandDoAfter : SimpleDoAfterEvent
{
}

public sealed partial class ChaplainHolyTouchActionEvent : EntityTargetActionEvent
{
}

