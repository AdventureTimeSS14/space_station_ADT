using System.Numerics;
using Content.Shared.ADT.TTS;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid.Components;

public sealed partial class RandomHumanoidAppearanceComponent
{
    [DataField]
    public List<ProtoId<TTSVoicePrototype>> Voices = new();

    [DataField]
    public List<ProtoId<MarkingPrototype>> HairStyles = new();

    [DataField]
    public List<Color> HairColors = new();

    [DataField]
    public bool ClearMarkings;

    [DataField]
    public Vector2? SkinTone;
}
