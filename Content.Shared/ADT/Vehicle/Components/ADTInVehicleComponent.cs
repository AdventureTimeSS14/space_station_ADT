using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTInVehicleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}
