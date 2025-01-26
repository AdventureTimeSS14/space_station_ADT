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

    /// <summary>
    /// Список языков, которые знает сущность. Писать в компонентах как:
    /// Прототип: Understand/BadSpeak/Speak
    /// </summary>
    [DataField("languages"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Dictionary<string, LanguageKnowledge> Languages = new();
}

[Serializable, NetSerializable]
public enum LanguageKnowledge : int
{
    Understand = 0,
    BadSpeak = 1,
    Speak = 2
}
