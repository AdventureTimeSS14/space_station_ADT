using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoonPassiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AmuletEquipped;
}
