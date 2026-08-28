using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TTS;

[Prototype("ttsVoice"), DataDefinition]
public sealed partial class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [DataField("description")]
    public string Description { get; private set; } = string.Empty;

    [DataField("sex", required: true)]
    public Sex Sex { get; private set; } = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("speaker", required: true)]
    public string Speaker { get; private set; } = string.Empty;

    [DataField("roundStart")]
    public bool RoundStart { get; private set; } = true;

    [DataField("sponsorOnly")]
    public bool SponsorOnly { get; private set; }

    [DataField("speciesBlacklist")]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesBlacklist { get; private set; } = new();

    [DataField("speciesWhitelist")]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesWhitelist { get; private set; } = new();
}
