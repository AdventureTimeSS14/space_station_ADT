using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Language;

/// <summary>
/// This component allows entity to speak and understand languages.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LanguageSpeakerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string? CurrentLanguage = default!;

    [DataField("languages"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<string, LanguageKnowledge> Languages = new();

    [DataField("speaks"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public List<string> SpokenLanguages = new();

    [DataField("understands"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public List<string> UnderstoodLanguages = new();
}

[Serializable, NetSerializable]
public enum LanguageKnowledge : int
{
    Understand = 0,
    BadSpeak = 1,
    Speak = 2
}
