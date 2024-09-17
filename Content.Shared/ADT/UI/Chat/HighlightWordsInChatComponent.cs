using Robust.Shared.GameStates;

namespace Content.Shared.ADT.UI.Chat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HighlightWordsInChatComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, DataField]
    public Dictionary<string, string[]> HighlightWords = new();
}
