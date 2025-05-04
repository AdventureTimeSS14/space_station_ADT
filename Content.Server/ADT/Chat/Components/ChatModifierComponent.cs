using Content.Shared.Chat;

namespace Content.Server.ADT.Chat;

/// <summary>
/// Компонент, позволяющий модифицировать дальность слышимости речи
/// </summary>
[RegisterComponent]
public sealed partial class ChatModifierComponent : Component
{
    [DataField("modifiers")]
    public Dictionary<ChatModifierType, int> Modifiers = new()
    {
        { ChatModifierType.Say, SharedChatSystem.VoiceRange },
        { ChatModifierType.WhisperClear, SharedChatSystem.WhisperClearRange },
        { ChatModifierType.WhisperMuffled, SharedChatSystem.WhisperMuffledRange },
    };
}

public enum ChatModifierType
{
    Say,
    WhisperClear,
    WhisperMuffled
}
