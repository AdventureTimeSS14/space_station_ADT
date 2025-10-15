using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage;

public sealed partial class AshDrakeSwoopActionEvent : InstantActionEvent
{
}

public sealed partial class AshDrakeMeteoritesActionEvent : InstantActionEvent
{
}

public sealed partial class AshDrakeFireActionEvent : WorldTargetActionEvent
{
}

public sealed partial class AshDrakeBreathActionEvent : WorldTargetActionEvent
{
}

[Serializable, NetSerializable]
public enum AshdrakeVisuals : byte
{
    Swoop,
}
