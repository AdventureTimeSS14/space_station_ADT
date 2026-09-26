using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.TTS;

[Prototype("ttsRadioChannel")]
public sealed partial class TTSRadioChannelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Effect { get; private set; } = "radio_headset";

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }
}
