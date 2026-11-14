using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ShieldBash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTShieldBashComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Distance = 1.5f;

    [DataField, AutoNetworkedField]
    public float RaisedDistance = 3f;

    [DataField, AutoNetworkedField]
    public float Speed = 7f;
}
