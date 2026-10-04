using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTVehicleRiderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}
