using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.AshWalker;

public sealed partial class ADTIgniteActionEvent : InstantActionEvent
{
}

public sealed partial class ADTHealTouchActionEvent : EntityTargetActionEvent
{
}

public sealed partial class ADTNecropolisCompassActionEvent : InstantActionEvent
{
}

[ByRefEvent]
public record struct ADTHealTouchUsedEvent(EntityUid User, bool Handled = false);

[Serializable, NetSerializable]
public sealed partial class ADTNecropolisCompassDoAfterEvent : SimpleDoAfterEvent
{
}
