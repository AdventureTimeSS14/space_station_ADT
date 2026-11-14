using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrucibleSoulStatusEffectComponent : Component
{
    [DataField]
    public EntityCoordinates? Coords;
}
