using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTChasmImmunityComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Until;
}
