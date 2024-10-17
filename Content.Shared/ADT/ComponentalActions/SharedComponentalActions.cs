using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.ComponentalActions;

public sealed partial class CompTeleportActionEvent : WorldTargetActionEvent
{
}

public sealed partial class CompProjectileActionEvent : WorldTargetActionEvent
{
}

public sealed partial class CompJumpActionEvent : WorldTargetActionEvent
{
}

public sealed partial class CompHealActionEvent : InstantActionEvent
{
}

public sealed partial class CompStasisHealActionEvent : InstantActionEvent
{
}

public sealed partial class CompInvisibilityActionEvent : InstantActionEvent
{
}

public sealed partial class CompElectrionPulseActionEvent : InstantActionEvent
{
}
