using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing.Badge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BadgeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string BadgeNumber = String.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string NotInDetailsText = "badge-cannot-be-seen-text";
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string InDetailsText = "badge-can-be-seen-text";
}
