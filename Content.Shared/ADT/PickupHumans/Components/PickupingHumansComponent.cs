using Content.Shared.ADT.Systems.PickupHumans;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent, AutoGenerateComponentState, Access(typeof(SharedPickupHumansSystem))]
public sealed partial class PickupingHumansComponent : Component
{
    public EntityUid User = default;

    [ViewVariables, DataField("sprintSpeedModifier"), AutoNetworkedField]
    public float SprintSpeedModifier = 0.7f;

    [ViewVariables, DataField("walkSpeedModifier"), AutoNetworkedField]
    public float WalkSpeedModifier = 0.6f;
}