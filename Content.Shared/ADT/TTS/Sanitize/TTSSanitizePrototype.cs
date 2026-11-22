using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.TTS.Sanitize;

[Prototype("ttsSanitize")]
public sealed partial class TTSSanitizePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Smaller numbers run earlier
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Lets a group be switched off without deleting it
    /// </summary>
    [DataField]
    public bool Enabled = true;

    [DataField]
    public List<ITTSSanitizeRule> Rules = new();

    public string Apply(string text)
    {
        foreach (var rule in Rules)
        {
            text = rule.Apply(text);

            if (text.Length == 0)
                return text;
        }

        return text;
    }
}
