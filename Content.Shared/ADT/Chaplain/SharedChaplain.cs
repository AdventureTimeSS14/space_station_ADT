using Content.Shared.Actions;
using Content.Shared.ADT.Chaplain.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Chaplain.Components;

public sealed partial class ChaplainMakeBelieverActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChaplainMakeBelieverDoAfter : SimpleDoAfterEvent
{
}

public sealed partial class ChaplainHolyShieldActionEvent : InstantActionEvent
{
}

public sealed partial class ChaplainExorcismActionEvent : EntityTargetActionEvent
{
}

public sealed partial class ChaplainBlessingActionEvent : EntityTargetActionEvent
{
}

public sealed partial class ChaplainHolyTouchActionEvent : EntityTargetActionEvent
{
}


[Serializable, NetSerializable]
public sealed class ChaplainPrayerRequestEvent : EntityEventArgs
{
    public readonly ProtoId<ChaplainPrayerPrototype> Prayer;

    public ChaplainPrayerRequestEvent(ProtoId<ChaplainPrayerPrototype> prayer)
    {
        Prayer = prayer;
    }
}
