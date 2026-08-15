using Content.Shared.Actions;

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
