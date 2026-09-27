using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PickupHumansComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> PickupHumansAlert = "ADTPickupHumans";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool InReadyPickupHumansMod;

    [DataField]
    public TimeSpan PickupTime = TimeSpan.FromSeconds(2f);

    [DataField]
    public int HandsRequired = 2;
}
