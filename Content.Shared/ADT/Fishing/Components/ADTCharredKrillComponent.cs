using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCharredKrillComponent : Component
{
    [DataField]
    public TimeSpan PlaceTime = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public bool InLava;
}
