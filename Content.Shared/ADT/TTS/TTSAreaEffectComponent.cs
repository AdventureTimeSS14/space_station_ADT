using Robust.Shared.GameStates;

namespace Content.Shared.ADT.TTS;

[RegisterComponent, NetworkedComponent]
public sealed partial class TTSAreaEffectComponent : Component
{
    [DataField(required: true)]
    public string Effect = string.Empty;
}
