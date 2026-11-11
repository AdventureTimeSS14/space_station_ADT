using Content.Shared.ADT.EMP;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpContainerProtectionComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, float> Batteries = new();

    [DataField]
    public string ContainerId = "cell_slot";

    [DataField]
    public float Multiplier = 0f;
}
