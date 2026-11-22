using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Sponsors.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SponsorGhostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
