using Content.Shared.ADT.TTS;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NPC;

[RegisterComponent]
public sealed partial class ADTRandomTTSComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<TTSVoicePrototype>> Voices = new();
}

[RegisterComponent]
public sealed partial class ADTRandomHairComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<MarkingPrototype>> Styles = new();

    [DataField(required: true)]
    public List<Color> Colors = new();

    [DataField]
    public bool ClearOtherMarkings = true;

    [DataField]
    public float MinSkinTone = 5f;

    [DataField]
    public float MaxSkinTone = 45f;
}
