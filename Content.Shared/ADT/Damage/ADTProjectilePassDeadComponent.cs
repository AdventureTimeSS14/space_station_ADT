using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Damage;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTProjectilePassDeadComponent : Component
{
    [DataField]
    public bool AllowTargeted = true;
}
