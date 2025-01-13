namespace Content.Shared.ADT.Language;

[ByRefEvent]
public record struct GetLanguagesEvent(EntityUid Uid)
{
    public string Current = "";
    public List<string> Spoken = new();
    public List<string> Understood = new();
    public List<string> TranslatorSpoken = new();
    public List<string> TranslatorUnderstood = new();
}
