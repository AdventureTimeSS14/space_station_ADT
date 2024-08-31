using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Server.Chat.Systems;

namespace Content.Server.Chat;

public sealed class AlternativeSpeechEvent : CancellableEntityEventArgs
{
    public string Message = String.Empty;
    public bool Radio;
    public InGameICChatType Type;

    public AlternativeSpeechEvent(string message, bool radio, InGameICChatType type)
    {
        Message = message;
        Radio = radio;
        Type = type;
    }
}
