using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Indestructible;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTIndestructibleComponent : Component
{
    [DataField]
    public bool BlockItemInteractions = true;

    [DataField]
    public bool AllowWelding;
}
