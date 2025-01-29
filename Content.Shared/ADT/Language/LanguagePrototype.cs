using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Language;

[Prototype("language")]
public sealed class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("obfuscateSyllables")]
    public bool ObfuscateSyllables { get; private set; } = false;

    [DataField]
    public Color? Color;

    [DataField]
    public Color? WhisperColor;

    [DataField("replacement", required: true)]
    public List<string> Replacement = new();

    [DataField]
    public int Priority = 1;

    [DataField]
    public bool Roundstart = false;

    public string LocalizedName => Loc.GetString("language-" + ID + "-name");

    public string LocalizedDescription => Loc.GetString("language-" + ID + "-description");
}
