using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonIllusionComponent : Component
{
    [DataField]
    public NetEntity? Caster;
}