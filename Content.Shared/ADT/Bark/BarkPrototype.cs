using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SpeechBarks;

[Prototype("speechBark")]
public sealed partial class BarkPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public bool RoundStart = true;

    [DataField]
    public string Name = "Default";

    [DataField(required: true)]
    public string Sound = "/Audio/Voice/Talk/speak_1.ogg";
}
