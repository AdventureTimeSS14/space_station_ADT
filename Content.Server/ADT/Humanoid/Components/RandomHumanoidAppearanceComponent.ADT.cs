using System.Numerics;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid.Components;

public sealed partial class RandomHumanoidAppearanceComponent
{
    [DataField]
    public List<ProtoId<TTSVoicePrototype>> Voices = new();

    [DataField]
    public List<ProtoId<TTSVoicePrototype>> FemaleVoices = new();

    [DataField]
    public List<ProtoId<MarkingPrototype>> HairStyles = new();

    [DataField]
    public List<ProtoId<MarkingPrototype>> FemaleHairStyles = new();

    [DataField]
    public List<Color> HairColors = new();

    [DataField]
    public bool ClearMarkings;

    [DataField]
    public bool ClearFemaleFacialHair;

    [DataField]
    public Vector2? SkinTone;

    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>> NameSegments = new();

    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>> FemaleNameSegments = new();

    [DataField]
    public LocId? NameFormat;

    [DataField]
    public ProtoId<LanguagePrototype>? Language;

    [DataField]
    public Sex? Sex;
}
