using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechVisuals : byte
{
    Open, //whether or not it's open and has a rider
    Broken //if it broke and no longer works.
}

[Serializable, NetSerializable]
public enum MechAssemblyVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum MechVisualLayers : byte
{
    Base
}

/// <summary>
/// Event raised on equipment when it is inserted into a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Event raised on equipment when it is removed from a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentRemovedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Raised on the mech equipment before it is going to be removed.
/// </summary>
[ByRefEvent]
public record struct AttemptRemoveMechEquipmentEvent()
{
    public bool Cancelled = false;
}

public sealed partial class MechToggleEquipmentEvent : InstantActionEvent
{
}

public sealed partial class MechOpenUiEvent : InstantActionEvent
{
}

public sealed partial class MechEjectPilotEvent : InstantActionEvent
{
}

// ADT Content start
public sealed partial class MechInhaleEvent : InstantActionEvent
{
}

public sealed partial class MechTurnLightsEvent : InstantActionEvent
{
}

/// <summary>
/// Raised on mech equipment destruction.
/// </summary>
[ByRefEvent]
public record struct MechEquipmentDestroyedEvent();

/// <summary>
/// Raised on the mech during pilot setup
/// </summary>
/// <param name="Mech"></param>
[ByRefEvent]
public record struct SetupMechUserEvent(EntityUid Pilot);

/// <summary>
/// Sent to server when player selects mech equipment in radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SelectMechEquipmentEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity? Equipment;

    public SelectMechEquipmentEvent(NetEntity user, NetEntity? equipment)
    {
        User = user;
        Equipment = equipment;
    }
}

[Serializable, NetSerializable]
public sealed partial class PopulateMechEquipmentMenuEvent : EntityEventArgs
{
    public List<NetEntity> Equipment;

    public PopulateMechEquipmentMenuEvent(List<NetEntity> equipment)
    {
        Equipment = equipment;
    }
}

/// <summary>
/// Exsists just to avoid exceptions but close radial menu on mech exit
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CloseMechMenuEvent : EntityEventArgs
{
}

// ADT Content end
