using Robust.Shared.GameStates;
namespace Content.Shared.ADT.Heretic.Components.Side;
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticHypnosisComponent : Component
{
    [DataField]
    public string Order = string.Empty;
}
