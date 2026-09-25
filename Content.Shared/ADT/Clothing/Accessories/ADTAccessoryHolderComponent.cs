using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing.Accessories;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAccessoryHolderComponent : Component
{
    [DataField]
    public string ContainerId = "adt-accessories";

    [DataField]
    public int MaxAccessories = 5;

    [ViewVariables]
    public Container Container = default!;
}
