using Robust.Shared.Prototypes;

namespace Content.Server.ADT.InconnuOS.NanoNet;

[Prototype("nanoNetFilter")]
public sealed partial class NanoNetFilterPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<string> Words = new();

    [DataField]
    public List<string> WholeWords = new();

    [DataField]
    public List<string> Phrases = new();

    [DataField]
    public List<string> FalsePositives = new();

    [DataField]
    public List<string> Patterns = new();
}
