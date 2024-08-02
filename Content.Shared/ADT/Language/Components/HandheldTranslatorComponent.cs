using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Language;

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
    [DataField("toggle")]
    public bool ToggleOnInteract = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Enabled = false;

}
