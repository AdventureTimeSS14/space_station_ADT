using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PickupingHumansComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Carried;

    [DataField, AutoNetworkedField]
    public float SprintSpeedModifier = 0.7f;

    [DataField, AutoNetworkedField]
    public float WalkSpeedModifier = 0.6f;
}
