using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Revolutinary.Stash;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTRevolutionStashComponent : Component
{
    [DataField]
    public string ContainerId = "adt_rev_stash";
}
