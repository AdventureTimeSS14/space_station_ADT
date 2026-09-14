using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Wizard.Guardian;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GuardianSharedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Host;
}