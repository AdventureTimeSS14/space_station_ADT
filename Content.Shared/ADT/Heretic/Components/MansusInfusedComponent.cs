//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MansusInfusedComponent : Component
{
    [DataField]
    public int MaxCharges = 1;

    [DataField]
    public int AvailableCharges = 1;

    [DataField]
    public string HeldPrefix = "infused";
}
