// Inspired by Nyanotrasen

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.CharecterFlavor;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class CharecterFlavorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string FlavorText = string.Empty;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string OOCNotes = string.Empty;
}
