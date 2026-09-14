using Content.Shared.Actions;

namespace Content.Shared.ADT.Hierophant;

public sealed partial class HierophantBlinkActionEvent : WorldTargetActionEvent
{
}

public sealed partial class HierophantChaserSwarmActionEvent : WorldTargetActionEvent
{
}

public sealed partial class HierophantCrossBlastsActionEvent : WorldTargetActionEvent
{
}

public sealed partial class HierophantBlinkSpamActionEvent : WorldTargetActionEvent
{
}

public sealed partial class HierophantVortexRecallActionEvent : InstantActionEvent
{
}

public sealed partial class HierophantToggleFriendlyFireActionEvent : InstantActionEvent
{
}
