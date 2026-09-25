using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Medical;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTHealingVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveEffect;
}