using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ArmorPenetration;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArmorPenetrationComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Penetration = 0.5f;
}
