using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Nutrition;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTFatComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.6f;
}