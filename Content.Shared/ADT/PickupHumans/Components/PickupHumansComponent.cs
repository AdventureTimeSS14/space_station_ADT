using Content.Shared.ADT.Systems.PickupHumans;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedPickupHumansSystem))]
public sealed partial class PickupHumansComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> PickupHumansAlert = "ADTPickupHumans";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool InReadyPickupHumansMod;

    [DataField]
    public TimeSpan PickupTime = TimeSpan.FromSeconds(2f);

    public int HandsRequired = 2;

    public EntityUid User = default;
    public EntityUid Target = default;
}