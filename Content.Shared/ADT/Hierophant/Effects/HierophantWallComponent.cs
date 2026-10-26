using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant.Effects;

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantWallComponent : Component
{
    [DataField]
    public EntityUid? Caster;
}
