using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

public sealed partial class MechInhaleEvent : InstantActionEvent
{
}

public sealed partial class MechTurnLightsEvent : InstantActionEvent
{
}

public sealed partial class MechOpenEquipmentRadialEvent : InstantActionEvent
{
}

public sealed partial class MechCycleEquipmentEvent : InstantActionEvent
{
}

public sealed partial class MechReloadEvent : InstantActionEvent
{
}

[ByRefEvent]
public record struct SetupMechUserEvent(EntityUid Pilot);

[ByRefEvent]
public record struct RemoveMechUserEvent(EntityUid Pilot);

[ByRefEvent]
public record struct MechSealToggledEvent(bool Sealed);

[Serializable, NetSerializable]
public sealed partial class SelectMechEquipmentEvent : EntityEventArgs
{
    public NetEntity Mech;
    public NetEntity? Equipment;

    public SelectMechEquipmentEvent(NetEntity mech, NetEntity? equipment)
    {
        Mech = mech;
        Equipment = equipment;
    }
}

[Serializable, NetSerializable]
public sealed partial class MechDrillDoAfterEvent : SimpleDoAfterEvent
{
}
