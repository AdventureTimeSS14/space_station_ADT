using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Language;

/// <summary>
/// This component allows holding entity to understand given languages if it can understand any of "toSpeak" languages
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HandheldTranslatorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("languages", required: true), AutoNetworkedField]
    public Dictionary<string, LanguageKnowledge> Languages;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toggle")]
    public bool ToggleOnInteract = true;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [DataField]
    public bool Enabled = false;

    public EntityUid? User;
}
