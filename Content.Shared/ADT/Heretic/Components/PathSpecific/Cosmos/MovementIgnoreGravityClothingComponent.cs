//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementIgnoreGravityClothingComponent : Component
{
    [DataField]
    public bool Weightless;
}
