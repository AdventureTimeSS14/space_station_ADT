namespace Content.Shared.ADT.TTS.Sanitize;

/// <summary>
/// A single step of the text preparation that happens before a phrase is handed to the speech service
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface ITTSSanitizeRule
{
    string Apply(string text);
}
