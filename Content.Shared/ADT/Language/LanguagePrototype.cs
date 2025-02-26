using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Language;

[Prototype("language")]
public sealed class LanguagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int Priority = 1;

    [DataField]
    public bool Roundstart = false;

    [DataField]
    public bool ShowUnderstood = true;

    [DataField]
    public Color? UiColor;

    public ILanguageType LanguageType
    {
        get
        {
            _languageType.Language = ID;
            return _languageType;
        }
        set => _languageType = value;
    }

    [DataField("speech", required: true, serverOnly: true)]
    private ILanguageType _languageType = null!;

    public string LocalizedName => Loc.GetString("language-" + ID + "-name");
    public string LocalizedDescription => Loc.GetString("language-" + ID + "-description");
}
