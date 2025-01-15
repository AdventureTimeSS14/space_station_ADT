using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage;

public sealed partial class AshDrakeSwoopActionEvent : InstantActionEvent
{
}

public sealed partial class AshDrakeMeteoritesActionEvent : InstantActionEvent
{
}

public sealed partial class AshDrakeFireActionEvent : EntityWorldTargetActionEvent
{
}

public sealed partial class AshDrakeBreathActionEvent : EntityWorldTargetActionEvent
{
}

[Serializable, NetSerializable]
public enum AshdrakeVisuals : byte
{
    Swoop,
}
