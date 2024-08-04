using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Language;

/// <summary>
/// This component allows holding entity to understand given languages if it can understand any of "toSpeak" languages
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HandheldTranslatorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toUnderstand", required: true)]
    public List<string> ToUnderstand;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toSpeak", required: true)]
    public List<string> ToSpeak;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requires", required: true)]
    public List<string> Required;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toggle")]
    public bool ToggleOnInteract = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Enabled = false;

}
