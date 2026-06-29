using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgFlashStunComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);
}


