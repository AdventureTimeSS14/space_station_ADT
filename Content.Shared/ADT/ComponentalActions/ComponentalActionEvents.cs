using Content.Shared.Actions;

namespace Content.Shared.ADT.ComponentalActions;

public sealed partial class CompTeleportActionEvent : WorldTargetActionEvent;

public sealed partial class CompProjectileActionEvent : WorldTargetActionEvent;

public sealed partial class CompJumpActionEvent : WorldTargetActionEvent;

public sealed partial class CompHealActionEvent : InstantActionEvent;

public sealed partial class CompStasisHealActionEvent : InstantActionEvent;

public sealed partial class CompInvisibilityActionEvent : InstantActionEvent;

public sealed partial class CompElectrionPulseActionEvent : InstantActionEvent;
