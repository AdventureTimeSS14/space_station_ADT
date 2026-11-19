using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechEquipmentDurabilityComponent : Component
{
    [DataField]
    public FixedPoint2 MaxDurability = 150;

    [DataField, AutoNetworkedField]
    public bool Broken;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MechPassiveEquipmentComponent : Component
{
}
