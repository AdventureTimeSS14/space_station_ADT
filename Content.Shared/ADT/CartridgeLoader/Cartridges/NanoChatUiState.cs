using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NanoChatUiState : BoundUserInterfaceState
{
    public readonly Dictionary<uint, NanoChatRecipient> Recipients = new();
    public readonly Dictionary<uint, List<NanoChatMessage>> Messages = new();
    public readonly Dictionary<uint, NanoChatGroup> Groups = new();
    public readonly Dictionary<uint, List<NanoChatMessage>> GroupMessages = new();
    public readonly List<NanoChatGroupInvite> Invites = new();
    public readonly List<NanoChatGroupInfo> PublicGroups = new();
    public readonly List<NanoChatRecipient>? Contacts;
    public readonly uint? CurrentChat;
    public readonly uint OwnNumber;
    public readonly int MaxRecipients;
    public readonly bool NotificationsMuted;
    public readonly bool ListNumber;

    public NanoChatUiState(
        Dictionary<uint, NanoChatRecipient> recipients,
        Dictionary<uint, List<NanoChatMessage>> messages,
        Dictionary<uint, NanoChatGroup> groups,
        Dictionary<uint, List<NanoChatMessage>> groupMessages,
        List<NanoChatGroupInvite> invites,
        List<NanoChatGroupInfo> publicGroups,
        List<NanoChatRecipient>? contacts,
        uint? currentChat,
        uint ownNumber,
        int maxRecipients,
        bool notificationsMuted,
        bool listNumber)
    {
        Recipients = recipients;
        Messages = messages;
        Groups = groups;
        GroupMessages = groupMessages;
        Invites = invites;
        PublicGroups = publicGroups;
        Contacts = contacts;
        CurrentChat = currentChat;
        OwnNumber = ownNumber;
        MaxRecipients = maxRecipients;
        NotificationsMuted = notificationsMuted;
        ListNumber = listNumber;
    }
}
