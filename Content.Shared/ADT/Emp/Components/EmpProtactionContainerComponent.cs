using Content.Shared.ADT.EMP;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.EMP;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpContainerProtectionComponent : Component
{
    public List<EntityUid> Batteries = new();

    [DataField]
    public string ContainerId = "cell_slot";
}
