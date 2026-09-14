using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechCockpitComponent : Component
{
    [DataField]
    public float EntryDelay = 3f;

    [DataField]
    public EntityWhitelist? PilotWhitelist;

    [DataField]
    public EntProtoId EjectAction = "ADTActionMechCockpitEject";

    [DataField, AutoNetworkedField]
    public EntityUid? EjectActionEntity;

    [DataField]
    public EntProtoId UiAction = "ActionMechOpenUI";

    [DataField, AutoNetworkedField]
    public EntityUid? UiActionEntity;

    [DataField]
    public EntProtoId CycleAction = "ActionMechCycleEquipment";

    [DataField, AutoNetworkedField]
    public EntityUid? CycleActionEntity;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MechControlLockedComponent : Component
{
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MechSecondPilotControlComponent : Component
{
}

[Serializable, NetSerializable]
public sealed partial class MechCockpitEntryEvent : SimpleDoAfterEvent
{
}
