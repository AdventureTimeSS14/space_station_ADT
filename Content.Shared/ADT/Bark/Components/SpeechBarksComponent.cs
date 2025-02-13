using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.SpeechBarks;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechBarksComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public BarkData Data = new();
}

