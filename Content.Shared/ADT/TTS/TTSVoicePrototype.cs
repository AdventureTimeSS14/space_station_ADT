using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TTS;

/// <summary>
/// Voice available for speech synthesis.
/// </summary>
[Prototype("ttsVoice")]
public sealed partial class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField(required: true)]
    public Sex Sex;

    /// <summary>
    /// Voice identifier on the speech service side.
    /// </summary>
    [DataField(required: true)]
    public string Speaker = string.Empty;

    /// <summary>
    /// Whether the voice can be picked in the character editor.
    /// </summary>
    [DataField]
    public bool RoundStart = true;

    [DataField]
    public bool SponsorOnly;

    /// <summary>
    /// Species that cannot use this voice.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesBlacklist = new();

    /// <summary>
    /// If not empty, only these species can use this voice.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesWhitelist = new();
}
