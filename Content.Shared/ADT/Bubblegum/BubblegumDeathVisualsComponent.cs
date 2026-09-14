using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumDeathVisualsComponent : Component
{
    [DataField]
    public TimeSpan ConvulseDuration = TimeSpan.FromSeconds(3.5);

    [DataField]
    public TimeSpan ImplodeDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public Color RageColor = Color.FromHex("#FF1010");

    [DataField]
    public Color HuskColor = Color.FromHex("#3D0E0E");

    [DataField]
    public float ToppleDegrees = 82f;
}
