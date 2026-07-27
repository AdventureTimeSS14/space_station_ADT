using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechCockpitPassengerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Mech;

    [ViewVariables, AutoNetworkedField]
    public EntityUid Cockpit;
}

public sealed partial class MechCockpitEjectEvent : InstantActionEvent
{
}
