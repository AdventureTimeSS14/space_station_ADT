using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Butchering;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCleanRemainsComponent : Component
{
    [DataField]
    public bool KeepOrgans = true;

    [DataField]
    public bool NoBloodSpill = true;
}
