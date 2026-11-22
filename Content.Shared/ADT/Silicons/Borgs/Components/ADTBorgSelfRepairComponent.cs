using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class ADTBorgSelfRepairComponent : Component
{
    [DataField]
    public EntProtoId WispProto = "ADTBorgRepairOrb";

    [DataField]
    public float HealAmount = 5f;

    [DataField]
    public float HealInterval = 5f;

    [DataField]
    public float EnergyCost = 5f;

    [DataField]
    public float ToggleCooldown = 5f;

    [ViewVariables]
    public bool Released;

    [ViewVariables]
    public EntityUid? Wisp;

    [ViewVariables]
    public TimeSpan NextToggleTime;
}

[RegisterComponent]
public sealed partial class ADTBorgSelfRepairActiveComponent : Component
{
    [ViewVariables]
    public TimeSpan NextHealTime;
}

public sealed partial class ADTBorgSelfRepairToggleAlertEvent : BaseAlertEvent;
