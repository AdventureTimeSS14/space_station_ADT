using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityActiveInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;
}
