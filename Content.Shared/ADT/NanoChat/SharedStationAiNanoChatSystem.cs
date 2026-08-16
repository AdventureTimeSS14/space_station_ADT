using Content.Shared.Actions;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.NanoChat;

[Serializable, NetSerializable]
public enum StationAiNanoChatUiKey : byte
{
    Key
}

public sealed partial class StationAiNanoChatActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class StationAiNanoChatUiMessage : BoundUserInterfaceMessage
{
    public readonly NanoChatUiMessageType Type;

    public readonly uint? RecipientNumber;

    public readonly string? Content;

    public readonly string? RecipientJob;

    public StationAiNanoChatUiMessage(NanoChatUiMessageType type,
        uint? recipientNumber = null,
        string? content = null,
        string? recipientJob = null)
    {
        Type = type;
        RecipientNumber = recipientNumber;
        Content = content;
        RecipientJob = recipientJob;
    }
}
