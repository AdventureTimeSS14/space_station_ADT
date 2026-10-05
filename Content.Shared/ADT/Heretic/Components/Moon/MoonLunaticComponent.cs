using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoonLunaticComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity Master;
}
