using Robust.Shared.GameStates;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LegCuffedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CuffedRSI = "ADT/Objects/Misc/legcuffs.rsi";

    [DataField, AutoNetworkedField]
    public string BodyIconState = "leg-irons";
}
