using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumSoulRiseComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2.2);

    [DataField]
    public float RiseHeight = 2.5f;
}
